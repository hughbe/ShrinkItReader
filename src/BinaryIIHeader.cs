using System.Buffers.Binary;
using System.Text;

namespace ShrinkItReader;

/// <summary>
/// Represents a Binary II header block.
/// Binary II is a wrapper format for ProDOS files, typically used for file transfers.
/// </summary>
public readonly struct BinaryIIHeader
{
    /// <summary>
    /// The size of the Binary II header in bytes.
    /// </summary>
    public const int Size = 128;

    /// <summary>
    /// The signature bytes identifying a Binary II header ($0A $47 $4C - linefeed, 'G', 'L').
    /// </summary>
    public static ReadOnlySpan<byte> Signature => [0x0A, 0x47, 0x4C];

    /// <summary>
    /// The ID byte at offset $12 that must be $02.
    /// </summary>
    public const byte IdByte = 0x02;

    /// <summary>
    /// Gets the ProDOS access byte.
    /// </summary>
    public byte Access { get; }

    /// <summary>
    /// Gets the ProDOS file type.
    /// </summary>
    public byte FileType { get; }

    /// <summary>
    /// Gets the ProDOS auxiliary type.
    /// </summary>
    public ushort AuxType { get; }

    /// <summary>
    /// Gets the ProDOS storage type.
    /// </summary>
    public byte StorageType { get; }

    /// <summary>
    /// Gets the size of the file in 512-byte blocks (includes ProDOS overhead).
    /// </summary>
    public ushort SizeInBlocks { get; }

    /// <summary>
    /// Gets the ProDOS modification date.
    /// </summary>
    public ushort ModificationDate { get; }

    /// <summary>
    /// Gets the ProDOS modification time.
    /// </summary>
    public ushort ModificationTime { get; }

    /// <summary>
    /// Gets the ProDOS creation date.
    /// </summary>
    public ushort CreationDate { get; }

    /// <summary>
    /// Gets the ProDOS creation time.
    /// </summary>
    public ushort CreationTime { get; }

    /// <summary>
    /// Gets the length of the file in bytes.
    /// </summary>
    public uint FileLength { get; }

    /// <summary>
    /// Gets the file name or partial pathname.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the GS/OS auxiliary type high word.
    /// </summary>
    public ushort GsosAuxTypeHigh { get; }

    /// <summary>
    /// Gets the GS/OS access high byte.
    /// </summary>
    public byte GsosAccessHigh { get; }

    /// <summary>
    /// Gets the GS/OS file type high byte.
    /// </summary>
    public byte GsosFileTypeHigh { get; }

    /// <summary>
    /// Gets the GS/OS storage type high byte.
    /// </summary>
    public byte GsosStorageTypeHigh { get; }

    /// <summary>
    /// Gets the GS/OS file size in blocks high word.
    /// </summary>
    public ushort GsosSizeInBlocksHigh { get; }

    /// <summary>
    /// Gets the GS/OS EOF high byte.
    /// </summary>
    public byte GsosEofHigh { get; }

    /// <summary>
    /// Gets the disk space needed for all files in this archive (set in first entry only).
    /// </summary>
    public uint DiskSpaceNeeded { get; }

    /// <summary>
    /// Gets the operating system type.
    /// </summary>
    public byte OperatingSystemType { get; }

    /// <summary>
    /// Gets the native file type (16 bits of OS-specific type data).
    /// </summary>
    public ushort NativeFileType { get; }

    /// <summary>
    /// Gets the phantom file flag.
    /// </summary>
    public byte PhantomFileFlag { get; }

    /// <summary>
    /// Gets the data flags (indicates compressed/encrypted/sparse).
    /// </summary>
    public byte DataFlags { get; }

    /// <summary>
    /// Gets the version number ($00 or $01).
    /// </summary>
    public byte Version { get; }

    /// <summary>
    /// Gets the number of files to follow (including phantoms).
    /// </summary>
    public byte FilesToFollow { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryIIHeader"/> struct.
    /// </summary>
    /// <param name="data">The raw data for the Binary II header.</param>
    /// <exception cref="ArgumentException">Thrown if the data is invalid.</exception>
    public BinaryIIHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Data must be at least {Size} bytes in length.", nameof(data));
        }

        // Validate signature: $0A $47 $4C (linefeed, 'G', 'L')
        if (!data.Slice(0, 3).SequenceEqual(Signature))
        {
            throw new ArgumentException("Invalid Binary II header signature.", nameof(data));
        }

        // Validate ID byte at offset $12 must be $02
        if (data[0x12] != IdByte)
        {
            throw new ArgumentException($"Invalid Binary II ID byte at offset $12. Expected {IdByte:X2}, got {data[0x12]:X2}.", nameof(data));
        }

        // +$03 / 1: ProDOS access byte
        Access = data[0x03];

        // +$04 / 1: ProDOS file type
        FileType = data[0x04];

        // +$05 / 2: ProDOS aux type
        AuxType = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x05, 2));

        // +$07 / 1: ProDOS storage type
        StorageType = data[0x07];

        // +$08 / 2: size of file, in 512-byte blocks
        SizeInBlocks = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x08, 2));

        // +$0A / 2: ProDOS modification date
        ModificationDate = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x0A, 2));

        // +$0C / 2: ProDOS modification time
        ModificationTime = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x0C, 2));

        // +$0E / 2: creation date
        CreationDate = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x0E, 2));

        // +$10 / 2: creation time
        CreationTime = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x10, 2));

        // +$14 / 3: length of file, in bytes (24-bit value)
        FileLength = (uint)(data[0x14] | (data[0x15] << 8) | (data[0x16] << 16));

        // +$17 / 65: file name or partial pathname, preceded by length byte; max 64 chars
        int fileNameLength = data[0x17];
        if (fileNameLength > 64)
        {
            fileNameLength = 64;
        }
        FileName = Encoding.ASCII.GetString(data.Slice(0x18, fileNameLength));

        // +$6D / 2: GS/OS aux type (high word)
        GsosAuxTypeHigh = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x6D, 2));

        // +$6F / 1: GS/OS access (high byte)
        GsosAccessHigh = data[0x6F];

        // +$70 / 1: GS/OS file type (high byte)
        GsosFileTypeHigh = data[0x70];

        // +$71 / 1: GS/OS storage type (high byte)
        GsosStorageTypeHigh = data[0x71];

        // +$72 / 2: GS/OS file size in blocks (high word)
        GsosSizeInBlocksHigh = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x72, 2));

        // +$74 / 1: GS/OS EOF (high byte)
        GsosEofHigh = data[0x74];

        // +$75 / 4: disk space needed for ALL files in this archive
        DiskSpaceNeeded = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0x75, 4));

        // +$79 / 1: operating system type
        OperatingSystemType = data[0x79];

        // +$7A / 2: native file type
        NativeFileType = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(0x7A, 2));

        // +$7C / 1: phantom file flag
        PhantomFileFlag = data[0x7C];

        // +$7D / 1: data flags
        DataFlags = data[0x7D];

        // +$7E / 1: version
        Version = data[0x7E];

        // +$7F / 1: number of files to follow
        FilesToFollow = data[0x7F];
    }

    /// <summary>
    /// Checks if the given data starts with a Binary II header signature.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data starts with a Binary II signature; otherwise, false.</returns>
    public static bool IsBinaryII(ReadOnlySpan<byte> data)
    {
        if (data.Length < Size)
        {
            return false;
        }

        // Check signature: $0A $47 $4C (linefeed, 'G', 'L')
        if (!data.Slice(0, 3).SequenceEqual(Signature))
        {
            return false;
        }

        // Check ID byte at offset $12 must be $02
        if (data[0x12] != IdByte)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the total size of this entry including the header and file data, padded to 128-byte boundary.
    /// </summary>
    public int TotalEntrySize
    {
        get
        {
            // Header (128 bytes) + file data padded to 128-byte boundary
            int paddedFileLength = ((int)FileLength + 127) & ~127;
            return Size + paddedFileLength;
        }
    }
}
