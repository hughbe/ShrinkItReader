using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.Contracts;
using System.Text;
using ShrinkItReader.Utilities;

namespace ShrinkItReader;

/// <summary>
/// Represents an ShrinkIt archive.
/// </summary>
public class ShrinkItArchive
{
    private readonly Stream _stream;

    private readonly long _streamStartOffset;

    /// <summary>
    /// Gets the Binary II header if this archive was wrapped in Binary II format.
    /// </summary>
    public BinaryIIHeader? BinaryIIHeader { get; }

    /// <summary>
    /// Gets the Master Header Block of the archive.
    /// </summary>
    public ShrinkItMasterHeaderBlock MasterHeaderBlock { get; }

    /// <summary>
    /// Gets the entries in the archive.
    /// </summary>
    public List<ShrinkItArchiveEntry> Entries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItArchive"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the ShrinkIt archive.</param>
    public ShrinkItArchive(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        _stream = stream;
        _streamStartOffset = stream.Position;

        // Read enough bytes to detect Binary II or NuFX header
        Span<byte> headerBuffer = stackalloc byte[ShrinkItReader.BinaryIIHeader.Size];
        if (stream.Read(headerBuffer) != headerBuffer.Length)
        {
            throw new ArgumentException("Stream is too small to contain a valid ShrinkIt archive.", nameof(stream));
        }

        // Check if this is a Binary II wrapped archive
        if (ShrinkItReader.BinaryIIHeader.IsBinaryII(headerBuffer))
        {
            BinaryIIHeader = new BinaryIIHeader(headerBuffer);
            
            // The NuFX archive starts immediately after the Binary II header
            // Read the NuFX master header block
            Span<byte> masterHeaderBlockBuffer = stackalloc byte[ShrinkItMasterHeaderBlock.Size];
            if (stream.Read(masterHeaderBlockBuffer) != masterHeaderBlockBuffer.Length)
            {
                throw new ArgumentException("Stream is too small to contain a valid ShrinkIt archive after Binary II header.", nameof(stream));
            }
            
            MasterHeaderBlock = new ShrinkItMasterHeaderBlock(masterHeaderBlockBuffer);
        }
        else
        {
            // Not Binary II - treat as raw NuFX archive
            // The header buffer already contains the master header block data
            MasterHeaderBlock = new ShrinkItMasterHeaderBlock(headerBuffer.Slice(0, ShrinkItMasterHeaderBlock.Size));
            
            // Seek back to after the master header block since we read 128 bytes but only needed 48
            stream.Seek(_streamStartOffset + ShrinkItMasterHeaderBlock.Size, SeekOrigin.Begin);
        }

        var entries = new List<ShrinkItArchiveEntry>();
        for (int i = 0; i < MasterHeaderBlock.TotalRecords; i++)
        {
            // Read an entry.
            var entry = new ShrinkItArchiveEntry(stream);
            entries.Add(entry);

            // Skip the entry data for now.
            stream.Seek(entry.DataLength, SeekOrigin.Current);
        }

        Entries = entries;
    }

    /// <summary>
    /// Gets the file name of the specified archive entry.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <returns>The file name of the archive entry.</returns>
    public string GetFileName(ShrinkItArchiveEntry entry)
    {
        if (ReadDecompressedData(entry, ShrinkItThreadClassification.FileName, 0x0000) is byte[] fileNameData)
        {
            return Encoding.ASCII.GetString(fileNameData);
        }

        return entry.FileName;
    }

    /// <summary>
    /// Gets the data fork of the specified archive entry.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <returns>The data fork of the archive entry.</returns>
    public byte[]? GetDataFork(ShrinkItArchiveEntry entry)
    {
        return ReadDecompressedData(entry, ShrinkItThreadClassification.Data, 0x0000);
    }

    /// <summary>
    /// Gets the disk image of the specified archive entry.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <returns>The disk image of the archive entry.</returns>
    public byte[]? GetDiskImage(ShrinkItArchiveEntry entry)
    {
        return ReadDecompressedData(entry, ShrinkItThreadClassification.Data, 0x0001);
    }

    /// <summary>
    /// Gets the resource fork of the specified archive entry.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <returns>The resource fork of the archive entry.</returns>
    public byte[]? GetResourceFork(ShrinkItArchiveEntry entry)
    {
        return ReadDecompressedData(entry, ShrinkItThreadClassification.Data, 0x0002);
    }

    /// <summary>
    /// Extracts the data fork of the specified archive entry directly to an output stream.
    /// This is more efficient than <see cref="GetDataFork"/> when writing to a file or other stream,
    /// as it avoids an intermediate buffer allocation.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <param name="outputStream">The stream to write the decompressed data to.</param>
    /// <returns>True if the data fork was extracted, false if the entry has no data fork.</returns>
    public bool ExtractDataForkTo(ShrinkItArchiveEntry entry, Stream outputStream)
    {
        return ExtractDecompressedDataTo(entry, ShrinkItThreadClassification.Data, 0x0000, outputStream);
    }

    /// <summary>
    /// Extracts the disk image of the specified archive entry directly to an output stream.
    /// This is more efficient than <see cref="GetDiskImage"/> when writing to a file or other stream,
    /// as it avoids an intermediate buffer allocation.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <param name="outputStream">The stream to write the decompressed data to.</param>
    /// <returns>True if the disk image was extracted, false if the entry has no disk image.</returns>
    public bool ExtractDiskImageTo(ShrinkItArchiveEntry entry, Stream outputStream)
    {
        return ExtractDecompressedDataTo(entry, ShrinkItThreadClassification.Data, 0x0001, outputStream);
    }

    /// <summary>
    /// Extracts the resource fork of the specified archive entry directly to an output stream.
    /// This is more efficient than <see cref="GetResourceFork"/> when writing to a file or other stream,
    /// as it avoids an intermediate buffer allocation.
    /// </summary>
    /// <param name="entry">The archive entry.</param>
    /// <param name="outputStream">The stream to write the decompressed data to.</param>
    /// <returns>True if the resource fork was extracted, false if the entry has no resource fork.</returns>
    public bool ExtractResourceForkTo(ShrinkItArchiveEntry entry, Stream outputStream)
    {
        return ExtractDecompressedDataTo(entry, ShrinkItThreadClassification.Data, 0x0002, outputStream);
    }

    private bool ExtractDecompressedDataTo(ShrinkItArchiveEntry entry, ShrinkItThreadClassification classification, ushort kind, Stream outputStream)
    {
        var offset = entry.DataOffset;

        foreach (var thread in entry.Threads)
        {
            if (thread.Classification == classification && thread.Kind == kind)
            {
                ExtractDecompressedDataTo(thread, offset, outputStream);
                return true;
            }

            offset += thread.CompressedDataSize;
        }

        return false;
    }

    private void ExtractDecompressedDataTo(ShrinkItThread thread, long offset, Stream outputStream)
    {
        _stream.Seek(offset, SeekOrigin.Begin);

        switch (thread.Format)
        {
            case ShrinkItThreadFormat.Uncompressed:
            {
                var toRead = (int)Math.Min(thread.UncompressedDataSize, thread.CompressedDataSize);
                CopyBytes(_stream, outputStream, toRead);
                
                // If uncompressed size is larger, pad with zeros
                var padding = (int)thread.UncompressedDataSize - toRead;
                if (padding > 0)
                {
                    Span<byte> zeros = stackalloc byte[Math.Min(padding, 4096)];
                    zeros.Clear();
                    while (padding > 0)
                    {
                        var toPad = Math.Min(padding, zeros.Length);
                        outputStream.Write(zeros[..toPad]);
                        padding -= toPad;
                    }
                }
                break;
            }
            case ShrinkItThreadFormat.DynamicLzw1:
            {
                DynamicLzw1Decompressor.DecompressToStream(_stream, outputStream, thread.CompressedDataSize, thread.UncompressedDataSize);
                break;
            }
            case ShrinkItThreadFormat.DynamicLzw2:
            {
                DynamicLzw2Decompressor.DecompressToStream(_stream, outputStream, thread.CompressedDataSize, thread.UncompressedDataSize);
                break;
            }
            default:
                throw new NotSupportedException($"Thread format {thread.Format} is not supported.");
        }
    }

    private static void CopyBytes(Stream source, Stream destination, int count)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int remaining = count;
            while (remaining > 0)
            {
                int toRead = Math.Min(remaining, buffer.Length);
                int read = source.Read(buffer, 0, toRead);
                if (read == 0)
                {
                    throw new InvalidOperationException("Stream is too small to contain all thread data.");
                }
                destination.Write(buffer, 0, read);
                remaining -= read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private byte[]? ReadDecompressedData(ShrinkItArchiveEntry entry, ShrinkItThreadClassification classification, ushort kind)
    {
        var offset = entry.DataOffset;

        foreach (var thread in entry.Threads)
        {
            if (thread.Classification == classification && thread.Kind == kind)
            {
                var memoryStream = new MemoryStream((int)thread.UncompressedDataSize);
                ExtractDecompressedDataTo(thread, offset, memoryStream);
                return memoryStream.ToArray();
            }

            offset += thread.CompressedDataSize;
        }

        return null;
    }
}
