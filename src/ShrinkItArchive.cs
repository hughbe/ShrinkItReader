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

        Span<byte> masterHeaderBlockBuffer = stackalloc byte[ShrinkItMasterHeaderBlock.Size];
        if (stream.Read(masterHeaderBlockBuffer) != masterHeaderBlockBuffer.Length)
        {
            throw new ArgumentException("Stream is too small to contain a valid ShrinkIt archive.", nameof(stream));
        }

        MasterHeaderBlock = new ShrinkItMasterHeaderBlock(masterHeaderBlockBuffer);

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

    private byte[]? ReadDecompressedData(ShrinkItArchiveEntry entry, ShrinkItThreadClassification classification, ushort kind)
    {
        var offset = entry.DataOffset;

        foreach (var thread in entry.Threads)
        {
            if (thread.Classification == classification && thread.Kind == kind)
            {
                return ReadDecompressedData(entry, thread, offset);
            }

            offset += thread.CompressedDataSize;
        }

        return null;
    }

    private byte[] ReadDecompressedData(ShrinkItArchiveEntry entry, ShrinkItThread thread, long offset)
    {
        _stream.Seek(offset, SeekOrigin.Begin);

        switch (thread.Format)
        {
            case ShrinkItThreadFormat.Uncompressed:
            {
                var buffer = new byte[thread.UncompressedDataSize];

                var toRead = Math.Min(buffer.Length, thread.CompressedDataSize);
                if (_stream.Read(buffer, 0, (int)toRead) != toRead)
                {
                    throw new InvalidOperationException("Stream is too small to contain all thread data.");
                }
                
                return buffer;
            }
            case ShrinkItThreadFormat.DynamicLzw1:
            {
                return DynamicLzw1Decompressor.Decompress(_stream, thread.CompressedDataSize, thread.UncompressedDataSize);
            }
            case ShrinkItThreadFormat.DynamicLzw2:
            {
                return DynamicLzw2Decompressor.Decompress(_stream, thread.CompressedDataSize, thread.UncompressedDataSize);
            }
            default:
                throw new NotSupportedException($"Thread format {thread.Format} is not supported.");
        }
    }
}
