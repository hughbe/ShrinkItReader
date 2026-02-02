using System.Buffers.Binary;
using System.Diagnostics;

namespace ShrinkItReader;

/// <summary>
/// The Thread structure used in ShrinkIt / NuFX archives.
/// </summary>
public readonly struct ShrinkItThread
{
    /// <summary>
    /// The size of the structure in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// Gets the classification of the thread.
    /// </summary>
    public ShrinkItThreadClassification Classification { get; }

    /// <summary>
    /// Gets the format of the thread.
    /// </summary>
    public ShrinkItThreadFormat Format { get; }

    /// <summary>
    /// Gets the kind of the thread.
    /// </summary>
    public ushort Kind { get; }

    /// <summary>
    /// Gets the 16-bit cyclic redundancy check (CRC) of the thread data.
    /// </summary>
    public ushort Crc { get; }

    /// <summary>
    /// Gets the size of the uncompressed data in bytes.
    /// </summary>
    public uint UncompressedDataSize { get; }

    /// <summary>
    /// Gets the size of the compressed data in bytes.
    /// </summary>
    public uint CompressedDataSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItThread"/> class from the specified data.
    /// </summary>
    /// <param name="data">The data containing the ShrinkIt Thread structure.</param>
    /// <exception cref="ArgumentException">Thrown when the data is not at least 16 bytes long.</exception>
    public ShrinkItThread(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Data must be at least {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://nulib.com/library/FTN.e08002.htm.
        int offset = 0;

        Classification = (ShrinkItThreadClassification)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        Format = (ShrinkItThreadFormat)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        Kind = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        Crc = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        UncompressedDataSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        CompressedDataSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        Debug.Assert(offset == data.Length, "Did not consume all bytes for ShrinkItThread structure.");
    }
}
