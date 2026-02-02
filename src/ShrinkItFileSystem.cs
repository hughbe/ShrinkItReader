namespace ShrinkItReader;

/// <summary>
/// The native file system identifiers used in ShrinkIt / NuFX headers.
/// Values are the 16-bit IDs stored in the archive headers.
/// </summary>
public enum ShrinkItFileSystem : ushort
{
    /// <summary>
    /// Unknown file system.
    /// </summary>
    Unknown = 0x0000,

    /// <summary>
    /// ProDOS / SOS file system.
    /// </summary>    
    ProDOS_SOS = 0x0001,

    /// <summary>
    /// DOS 3.3 file system.
    /// </summary>
    DOS_3_3 = 0x0002,

    /// <summary>
    /// DOS 3.2 file system.
    /// </summary>
    DOS_3_2 = 0x0003,

    /// <summary>
    /// Apple II Pascal file system.
    /// </summary>
    AppleII_Pascal = 0x0004,

    /// <summary>
    /// Macintosh HFS file system.
    /// </summary>
    Macintosh_HFS = 0x0005,

    /// <summary>
    /// Macintosh MFS file system.
    /// </summary>
    Macintosh_MFS = 0x0006,

    /// <summary>
    /// Lisa File System.
    /// </summary>
    Lisa_File_System = 0x0007,

    /// <summary>
    /// Apple CP/M file system.
    /// </summary>
    Apple_CP_M = 0x0008,

    /// <summary>
    /// Reserved.
    /// </summary>
    Reserved_0x0009 = 0x0009,

    /// <summary>
    /// MS-DOS file system.
    /// </summary>
    MS_DOS = 0x000A,

    /// <summary>
    /// High Sierra file system.
    /// </summary>
    High_Sierra = 0x000B,

    /// <summary>
    /// ISO 9660 file system.
    /// </summary>
    ISO_9660 = 0x000C,

    /// <summary>
    /// AppleShare file system.
    /// </summary>
    AppleShare = 0x000D,
}
