using System.Buffers.Binary;

namespace ShrinkItReader;

/// <summary>
/// Represents a GS/OS option list structure.
/// </summary>
public class GSOSOptionList
{
    /// <summary>
    /// The minimum size of a GS/OS option list structure in bytes.
    /// </summary>
    public const int MinSize = 6;

    /// <summary>
    /// Gets the buffer size specified in the option list.
    /// </summary>
    public ushort BufferSize { get; }

    /// <summary>
    /// Gets the list size specified in the option list.
    /// </summary>
    public ushort ListSize { get; }

    /// <summary>
    /// Gets the file system identifier specified in the option list.
    /// </summary>
    public ShrinkItFileSystem FileSystem { get; }

    /// <summary>
    /// Gets the option data bytes stored in the option list.
    /// </summary>
    public byte[] OptionData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GSOSOptionList"/> class from the specified data.
    /// </summary>
    /// <param name="data">The data containing the GS/OS option list structure.</param>
    /// <exception cref="ArgumentException">The data is smaller than the minimum size required for a GS/OS option list.</exception>
    public GSOSOptionList(ReadOnlySpan<byte> data)
    {
        if (data.Length < MinSize)
        {
            throw new ArgumentException($"Data must be at least {MinSize} bytes long.", nameof(data));
        }

        // Structure documented in https://nulib.com/library/FTN.e08002.htm
        int offset = 0;

        // Size of the buffer for GS/OS to place the option_list in, including this
        // count word. This must be at least $2E.
        BufferSize = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        if (BufferSize < 0x2E)
        {
            throw new ArgumentException("Data does not contain a valid GS/OS option list buffer size.", nameof(data));
        }
        if (BufferSize > data.Length)
        {
            throw new ArgumentException("Data does not contain the full GS/OS option list buffer.", nameof(data));
        }

        // The number of bytes of information  returned by GS/OS.
        ListSize = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // A file system ID word (see list above) identifying the FST owning the
        // file in question.
        FileSystem = (ShrinkItFileSystem)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // The bytes returned by the FST.  There are (buffer_size - 6) of them.
        OptionData = new byte[BufferSize - 6];
        data.Slice(offset, OptionData.Length).CopyTo(OptionData);
    }
}
