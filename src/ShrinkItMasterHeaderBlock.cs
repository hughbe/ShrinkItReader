using System.Buffers.Binary;
using System.Diagnostics;
using ShrinkItReader.Utilities;

namespace ShrinkItReader;

/// <summary>
/// The Master Header Block structure used in ShrinkIt archives.
/// </summary>
public readonly struct ShrinkItMasterHeaderBlock
{
    /// <summary>
    /// The size of the structure in bytes.
    /// </summary>
    public const int Size = 48;

    /// <summary>
    /// Gets the signature bytes identifying the Master Header Block.
    /// </summary>
    public ByteArray6 Signature { get; }

    /// <summary>
    /// Gets the 16-bit cyclic redundancy check (CRC) of the remaining fields in this block.
    /// </summary>
    public ushort Crc { get; }

    /// <summary>
    /// Gets the total number of records in this archive file.
    /// </summary>
    public uint TotalRecords { get; }

    /// <summary>
    /// Gets the date and time on which this archive was initially created.
    /// </summary>
    public ShrinkItDateTime CreationDate { get; }

    /// <summary>
    /// Gets the date of the last modification to this archive.
    /// </summary>
    public ShrinkItDateTime LastModificationDate { get; }

    /// <summary>
    /// Gets the version number of this archive.
    /// </summary>
    public ushort VersionNumber { get; }

    /// <summary>
    /// Gets the reserved bytes (must be null).
    /// </summary>
    public ByteArray8 Reserved1 { get; }

    /// <summary>
    /// Gets the total size of the archive in bytes.
    /// </summary>
    public uint TotalSize { get; }

    /// <summary>
    /// Gets the reserved bytes.
    /// </summary>    
    public ByteArray6 Reserved2 { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItMasterHeaderBlock"/> struct.
    /// </summary>
    /// <param name="data">The raw data for the Master Header Block.</param>
    /// <exception cref="ArgumentException">Thrown if the data is invalid.</exception>
    public ShrinkItMasterHeaderBlock(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        // Structure documented in https://nulib.com/library/FTN.e08002.htm
        int offset = 0;

        // These six bytes spell the word "NuFile" in alternating ASCII (low, then
        // high) for uniqueness. The six bytes are $4E $F5 $46 $E9 $6C $E5.
        Signature = new ByteArray6(data.Slice(offset, ByteArray6.Size));
        offset += ByteArray6.Size;

        ReadOnlySpan<byte> expectedSignature =
        [
            0x4E, 0xF5, 0x46, 0xE9, 0x6C, 0xE5
        ];
        if (!Signature.AsSpan().SequenceEqual(expectedSignature))
        {
            throw new ArgumentException("Invalid Master Header Block signature.", nameof(data));
        }

        // A 16-bit cyclic redundancy check (CRC) of the remaining fields in this 
        // block (bytes +008 through +047). Any programs which modify the master
        // header block must recalculate the CRC for the  master header. (see the
        // section "A Sample  CRC Algorithm")  The initial value of this  CRC is
        // $0000.
        Crc = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // The total number of records in this  archive file. It is possible to
        // chain multiple records (files or disks) together, as it is possible
        // to chain different types of records together (mixed files and disks).
        TotalRecords = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        // The date and time on which this archive was initially created. This
        // field should never be changed once initially written. If the date is
        // not known, or is unable to be calculated, this field should be set to
        // zero.  If the weekday is not known, or is unable to be calculated,
        // this field should be set to null.
        CreationDate = new ShrinkItDateTime(data.Slice(offset, ShrinkItDateTime.Size));
        offset += ShrinkItDateTime.Size;

        // The date of the last modification to this archive. This field should
        // be changed every time a change is made to any of the records in the
        // archive. If the date is not known, or is unable to be calculated, this
        // field should be set to zero. If the weekday is not known, or is unable
        // to be calculated, this field should be set to null.
        LastModificationDate = new ShrinkItDateTime(data.Slice(offset, ShrinkItDateTime.Size));
        offset += ShrinkItDateTime.Size;

        // The master version number of the NuFX archive. This Note describes
        // master_version $0002, for which the next eight bytes are zeroed.
        VersionNumber = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        if (VersionNumber > 2)
        {
            throw new ArgumentException($"Unsupported Master Header Block version number: {VersionNumber}.", nameof(data));
        }

        // Must be null ($00000000).
        Reserved1 = new ByteArray8(data.Slice(offset, ByteArray8.Size));
        offset += ByteArray8.Size;

        // The length of the NuFX archive, in bytes. Any programs which modify the
        // length of an archive, either increasing it or decreasing it in size, must
        // change this field in the master header to reflect the new size.
        TotalSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // Not documented but appears to be reserved.
        Reserved2 = new ByteArray6(data.Slice(offset, ByteArray6.Size));
        offset += ByteArray6.Size;

        Debug.Assert(offset == data.Length, "Did not consume all bytes for MasterHeaderBlock.");
    }
}
