using System.Buffers.Binary;
using System.Diagnostics;
using ProDosVolumeReader;
using ShrinkItReader.Utilities;

namespace ShrinkItReader;

/// <summary>
/// The Header Block structure used in ShrinkIt archives.
/// </summary>
public readonly struct ShrinkItHeaderBlock
{
    /// <summary>
    /// The size of the structure in bytes.
    /// </summary>
    public const int Size = 56;

    /// <summary>
    /// Gets the signature bytes identifying the Header Block.
    /// </summary>
    public ByteArray4 Signature { get; }

    /// <summary>
    /// Gets the CRC of the Header Block.
    /// </summary>
    public ushort Crc { get; }

    /// <summary>
    /// Gets the length of the attribute section in bytes.
    /// </summary>
    public ushort AttributesCount { get; }
    
    /// <summary>
    /// Gets the version number of this record.
    /// </summary>
    public ushort VersionNumber { get; }

    /// <summary>
    /// Gets the total number of threads in this record.
    /// </summary>
    public uint TotalThreads { get; }

    /// <summary>
    /// Gets the native file system identifier.
    /// </summary>
    public ShrinkItFileSystem FileSystemId { get; }

    /// <summary>
    /// Gets information about the current filing system.
    /// </summary>
    public ushort FileSystemInfo { get; }

    /// <summary>
    /// Gets the access flags for the file.
    /// </summary>
    public ShrinkItHeaderBlockAccessFlags AccessFlags { get; }

    /// <summary>
    /// Gets the file type of the file being archived.
    /// </summary>
    public FileType FileType { get; }

    /// <summary>
    /// Gets the auxiliary type of the file being archived.
    /// </summary>
    public uint AuxType { get; }

    /// <summary>
    /// Gets the storage type of the file or block size for disks.
    /// </summary>
    public ushort StorageTypeOrBlockSize { get; }

    /// <summary>
    /// Gets the storage type of the file.
    /// </summary>
    public StorageType StorageType => (StorageType)(StorageTypeOrBlockSize & 0x00FF);

    /// <summary>
    /// Gets the block size for disks.
    /// </summary>
    public ushort BlockSize => (ushort)StorageTypeOrBlockSize;

    /// <summary>
    /// Gets the creation date and time of the file.
    /// </summary>
    public ShrinkItDateTime CreationDate { get; }

    /// <summary>
    /// Gets the last modification date and time of the file.
    /// </summary>
    public ShrinkItDateTime LastModificationDate { get; }

    /// <summary>
    /// Gets the date and time this record was archived.
    /// </summary>
    public ShrinkItDateTime ArchiveDate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItHeaderBlock"/> struct.
    /// </summary>
    /// <param name="data">The raw data for the Header Block.</param>
    /// <exception cref="ArgumentException">Thrown if the data is invalid.</exception>
    public ShrinkItHeaderBlock(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://nulib.com/library/FTN.e08002.htm
        int offset = 0;

        // These four bytes spell the word "NuFX" in alternating ASCII (low, then
        // high) for uniqueness. The four bytes are $4E $F5 $46 $D8.
        Signature = new ByteArray4(data.Slice(offset, 4));
        offset += 4;

        ReadOnlySpan<byte> expectedSignature = stackalloc byte[] { 0x4E, 0xF5, 0x46, 0xD8 };
        if (!Signature.AsSpan().SequenceEqual(expectedSignature))
        {
            throw new ArgumentException("Data does not contain a valid Header Block signature.", nameof(data));
        }

        // The 16-bit CRC of the remaining fields of this block (bytes +006 through
        // the end of the header block and any threads following it). This field is
        // used to verify the integrity of the rest of the block. Programs which
        // create NuFX archives must include this in every header. It is up to the
        // discretion of the extracting program to check the validity of this CRC.
        // Any programs which might modify the header of a particular record must
        // recalculate the CRC for the header block. The initial value for this
        // CRC is zero ($0000).
        Crc = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // This field describes the length of the attribute section of each record
        // in bytes. This count measures the distance in bytes from the first field
        // (offset +000) up to and including the filename_length field. By convention,
        // the filename_length field will always be the last 2 bytes of the attribute
        // section regardless of what has preceded it.This field describes the length
        // of the attribute section of each record in bytes.  This count measures the
        // distance in bytes from the first field (offset +000) up to and including
        // the filename_length field.  By convention, the filename_length field will
        // always be the last 2 bytes of the attribute section regardless of what
        // has preceded it.
        AttributesCount = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        if (AttributesCount < Size)
        {
            throw new ArgumentException("Data does not contain a valid Header Block attribute count.", nameof(data));
        }

        // Version of this record. If version_number is $0000, no option_list fields
        // are present. If the version_number is $0001 option_list fields may be
        // present. If the version_number is $0002 then option_list fields may be
        // present and a valid CRC-16 exists for the compressed data in the data
        // threads of this record. If the version_number is $0003 then option_list
        // fields may be present and a valid CRC-16 exists for the uncompressed data
        // in the data threads of this record. The current version number is $0003
        // and should always be used when making archives.
        VersionNumber = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // The number of thread subrecords which should be expected immediately
        // following the filename or pathname at the end of this header block. This
        // field is extremely important as it contains the information about the
        // length of the last third of the header.
        TotalThreads = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        // The native file system identifier:
        // $0000    reserved
        // $0001    ProDOS/SOS
        // $0002    DOS 3.3
        // $0003    DOS 3.2
        // $0004    Apple II Pascal
        // $0005    Macintosh HFS
        // $0006    Macintosh MFS
        // $0007    Lisa File System
        // $0008    Apple CP/M
        // $0009    reserved, do not use (The GS/OS Character FST returns this value)
        // $000A    MS-DOS
        // $000B    High Sierra
        // $000C    ISO 9660
        // $000D    AppleShare
        // $000E-$FFFF    Reserved, do not use
        // If the file system of a disk being archived is not known, it should be
        // set to zero.
        FileSystemId = (ShrinkItFileSystem)BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // Information about the current filing system. The low byte of this word
        // (offset +016) is the native file system separator. For ProDOS, this is
        // the slash (/ or $2F). For HFS and GS/OS, the colon (: or $3F) is used,
        // and for MS-DOS, the separator is the backslash (\ or $5C). This
        // separator is provided so archival utilities may know how to parse a
        // valid file or pathname from the filename field for the receiving file.
        // GS/OS archival utilities should not attempt to parse pathnames, as it
        // is not possible to build in syntax rules for file systems not currently
        // defined. Instead, pass the pathname directory to GS/OS and attempt
        // translation (asking the user for suggestions) only if GS/OS returns
        // an "Invalid Path Name Syntax" error.  The high byte of this word is
        // reserved and should remain zero.
        FileSystemInfo = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // Bits 31-8    reserved, must be zero
        // Bit 7 (D)    1 = destroy enabled
        // Bit 6 (R)    1 = rename enabled
        // Bit 5 (B)    1 = file needs to be backed up
        // Bits 4-3     reserved, must be zero
        // Bit 2 (I)    1 = file is invisible
        // Bit 1 (W)    1 = write enabled
        // Bit 0 (R)    1 = read enabled
        AccessFlags = (ShrinkItHeaderBlockAccessFlags)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        // The file type of the file being archived. For ProDOS 8 or GS/OS, this
        // field should always be what the operating system returns when asked.
        // For disks being archived, this field should be zero.
        FileType = (FileType)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        // The auxiliary type of the file being archived. For ProDOS 8 or GS/OS,
        // this field should always be what the operating system returns when asked.
        // For disks being archived, this field should be the total number of blocks
        // on the disk.
        AuxType = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        offset += 4;

        // For Files: The storage type of the file. Types $1 through $3 are
        // standard (one-forked) files, type $5 is an extended (two-forked) file,
        // and type $D is a subdirectory.
        // For Disks: The block size used by the device should be placed in this
        // field. For example, under ProDOS, this field will be 512, while for HFS
        // it might be 524. The GS/OS Volume call will return this information if
        // asked.
        StorageTypeOrBlockSize = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        offset += 2;

        // The date and time on which this record was initially created. If the
        // creation date and time are available from a disk device, this information
        // should be included. If the date is not known, or is unable to be
        // calculated, this field should be set to zero. If the weekday is not
        // known, or is unable to be calculated, this field should be set to zero.
        CreationDate = new ShrinkItDateTime(data.Slice(offset, ShrinkItDateTime.Size));
        offset += ShrinkItDateTime.Size;

        // The date and time on which this record was last modified. If the
        // modification date and time are available from a disk device, this
        // information should be included. If the date is not known, or is unable
        // to be calculated, this field should be set to zero. If the weekday is not
        // known, or is unable to be calculated, this field should be set to zero.
        LastModificationDate = new ShrinkItDateTime(data.Slice(offset, ShrinkItDateTime.Size));
        offset += ShrinkItDateTime.Size;

        // The date and time on which this record was placed in this archive. If
        // the date is not known, or is unable to be calculated, this field should
        // be set to zero. If the weekday is not known, or is unable to be
        // calculated, this field should be set to zero.
        ArchiveDate = new ShrinkItDateTime(data.Slice(offset, ShrinkItDateTime.Size));
        offset += ShrinkItDateTime.Size;

        Debug.Assert(offset == data.Length, "Did not consume all data for HeaderBlock");
    }
}
