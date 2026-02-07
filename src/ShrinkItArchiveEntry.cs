using System.Buffers.Binary;
using System.Text;
using ShrinkItReader.Utilities;

namespace ShrinkItReader;

/// <summary>
/// An entry in a ShrinkIt archive.
/// </summary>
public class ShrinkItArchiveEntry
{
    /// <summary>
    /// Gets the offset of the archive entry within the archive stream.
    /// </summary>
    public long Offset { get; }

    /// <summary>
    /// Gets the offset of the data section of the archive entry within the archive stream.
    /// </summary>
    public long DataOffset { get; }

    /// <summary>
    /// Gets the length of the data section of the archive entry.
    /// </summary>
    public long DataLength { get; }

    /// <summary>
    /// Gets the Header Block of the archive entry.
    /// </summary>
    public ShrinkItHeaderBlock HeaderBlock { get; }

    /// <summary>
    /// Gets the length of the option list of the archive entry.
    /// </summary>
    public ushort OptionListLength { get; }

    /// <summary>
    /// Gets the option list data of the archive entry.
    /// </summary>
    public byte[]? OptionList { get; }

    /// <summary>
    /// Gets the extra data of the archive entry.
    /// </summary>
    public byte[]? ExtraData { get; }

    /// <summary>
    /// Gets the length of the file name of the archive entry.
    /// </summary>
    public ushort FileNameLength { get; }

    /// <summary>
    /// Gets the file name of the archive entry.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the threads associated with the archive entry.
    /// </summary>
    public List<ShrinkItThread> Threads { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItArchiveEntry"/> class.
    /// </summary>
    /// <param name="stream">The stream representing the ShrinkIt archive.</param>
    /// <exception cref="ArgumentException">Thrown if the stream is too small to contain all required data.</exception>
    public ShrinkItArchiveEntry(Stream stream)
    {
        Offset = stream.Position;

        // Read the Header Block
        Span<byte> buffer = stackalloc byte[ShrinkItHeaderBlock.Size];
        if (stream.Read(buffer) != buffer.Length)
        {
            throw new ArgumentException("Stream is too small to contain all header blocks.", nameof(stream));
        }

        HeaderBlock = new ShrinkItHeaderBlock(buffer);
        var bytesRead = ShrinkItHeaderBlock.Size;

        // The following option_list information is only present if the NuFX
        // version number for this record is $0001 or greater.
        // The length of the FST-specific portion of a GS/OS option_list
        // returned by GS/OS. This field may be $0000, indicating the
        // absence of a valid option_list.
        if (HeaderBlock.VersionNumber >= 1)
        {
            if (stream.Read(buffer[..2]) != 2)
            {
                throw new ArgumentException("Stream is too small to contain extended header block data.", nameof(stream));
            }

            OptionListLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[..2]);
            bytesRead += 2;

            if (OptionListLength > 0)
            {
                var optionListData = new byte[OptionListLength];
                if (stream.Read(optionListData) != optionListData.Length)
                {
                    throw new ArgumentException("Stream is too small to contain full option list data.", nameof(stream));
                }

                OptionList = optionListData;
                bytesRead += optionListData.Length;
            }
            else
            {
                OptionList = [];
            }
        }
        else
        {
            OptionListLength = 0;
            OptionList = null;
        }

        // Because the attributes section does not have a fixed size, the next
        // field must be found by looking two bytes before the offset indicated
        // by attrib_count (+006).
        var extraHeaderDataLength = HeaderBlock.AttributesCount - 2 - bytesRead;
        if (extraHeaderDataLength > 0)
        {
            var extraHeaderData = new byte[extraHeaderDataLength];
            if (stream.Read(extraHeaderData) != extraHeaderData.Length)
            {
                throw new ArgumentException("Stream is too small to contain full extra header data.", nameof(stream));
            }

            ExtraData = extraHeaderData;
        }
        else
        {
            ExtraData = null;
        }

        // Obsolete, should be set to zero. In previous versions of NuFX, this
        // field was the length of a file name or pathname immediately following
        // this field. To allow the inclusion of future additional parameters
        // in the attributes section, NuFX utility programs should rely on the
        // attribs_count field to find the filename_length field. Current
        // convention is to zero this field when building an archive and put
        // the file or pathname into a filename thread so the record can be
        // renamed in the archive. Archival programs should recognize both
        // methods to find a valid file name or pathname.
        if (stream.Read(buffer[..2]) != 2)
        {
            throw new ArgumentException("Stream is too small to contain filename length data.", nameof(stream));
        }

        FileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[..2]);
        if (FileNameLength > 0)
        {
            var filenameData = new byte[FileNameLength];
            if (stream.Read(filenameData) != filenameData.Length)
            {
                throw new ArgumentException("Stream is too small to contain full filename data.", nameof(stream));
            }

            FileName = Encoding.ASCII.GetString(filenameData);
        }
        else
        {
            FileName = string.Empty;
        }

        // Thread Records are 16-byte records which immediately follow the Header Block
        // (composed of the attributes and file name of the current record) and describe
        // the types of data structures which are included with a given record. The
        // number of Thread Records is described in the attribute section by a Word,
        // total_threads.
        long totalThreadDataSize = 0;
        var threads = new List<ShrinkItThread>((int)HeaderBlock.TotalThreads);
        for (int threadIndex = 0; threadIndex < HeaderBlock.TotalThreads; threadIndex++)
        {
            if (stream.Read(buffer[..ShrinkItThread.Size]) != ShrinkItThread.Size)
            {
                throw new ArgumentException("Stream is too small to contain all thread records.", nameof(stream));
            }

            var thread = new ShrinkItThread(buffer[..ShrinkItThread.Size]);
            totalThreadDataSize += thread.CompressedDataSize;
            threads.Add(thread);
        }

        Threads = threads;

        // Data section immediately follows the thread records.
        DataOffset = stream.Position;
        DataLength = totalThreadDataSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItArchiveEntry"/> class from a memory span.
    /// This avoids stream overhead when parsing from in-memory data.
    /// </summary>
    /// <param name="data">The span of data starting at this entry.</param>
    /// <param name="baseOffset">The absolute offset of this entry in the archive.</param>
    internal ShrinkItArchiveEntry(ReadOnlySpan<byte> data, long baseOffset)
    {
        Offset = baseOffset;
        int pos = 0;

        // Read the Header Block
        HeaderBlock = new ShrinkItHeaderBlock(data.Slice(pos, ShrinkItHeaderBlock.Size));
        pos += ShrinkItHeaderBlock.Size;
        var bytesRead = ShrinkItHeaderBlock.Size;

        if (HeaderBlock.VersionNumber >= 1)
        {
            OptionListLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pos, 2));
            pos += 2;
            bytesRead += 2;

            if (OptionListLength > 0)
            {
                OptionList = data.Slice(pos, OptionListLength).ToArray();
                pos += OptionListLength;
                bytesRead += OptionListLength;
            }
            else
            {
                OptionList = [];
            }
        }
        else
        {
            OptionListLength = 0;
            OptionList = null;
        }

        var extraHeaderDataLength = HeaderBlock.AttributesCount - 2 - bytesRead;
        if (extraHeaderDataLength > 0)
        {
            ExtraData = data.Slice(pos, extraHeaderDataLength).ToArray();
            pos += extraHeaderDataLength;
        }
        else
        {
            ExtraData = null;
        }

        FileNameLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pos, 2));
        pos += 2;

        if (FileNameLength > 0)
        {
            FileName = Encoding.ASCII.GetString(data.Slice(pos, FileNameLength));
            pos += FileNameLength;
        }
        else
        {
            FileName = string.Empty;
        }

        long totalThreadDataSize = 0;
        var threads = new List<ShrinkItThread>((int)HeaderBlock.TotalThreads);
        for (int threadIndex = 0; threadIndex < HeaderBlock.TotalThreads; threadIndex++)
        {
            var thread = new ShrinkItThread(data.Slice(pos, ShrinkItThread.Size));
            totalThreadDataSize += thread.CompressedDataSize;
            threads.Add(thread);
            pos += ShrinkItThread.Size;
        }

        Threads = threads;

        DataOffset = baseOffset + pos;
        DataLength = totalThreadDataSize;
    }
}
