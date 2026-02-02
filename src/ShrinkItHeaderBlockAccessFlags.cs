namespace ShrinkItReader;

/// <summary>
/// Access flags stored in the Header Block access field.
/// Bits 31-8: reserved (must be zero).
/// Bit 7 (D): destroy enabled
/// Bit 6 (R): rename enabled
/// Bit 5 (B): file needs to be backed up
/// Bits 4-3: reserved
/// Bit 2 (I): file is invisible
/// Bit 1 (W): write enabled
/// Bit 0 (R): read enabled
/// </summary>
[System.Flags]
public enum ShrinkItHeaderBlockAccessFlags : uint
{
    /// <summary>
    /// No access flags set.
    /// </summary>
    None = 0u,

    /// <summary>
    /// Read access.
    /// </summary>
    Read = 1u << 0,

    /// <summary>
    /// Write access.
    /// </summary>
    Write = 1u << 1,

    /// <summary>
    /// File is invisible.
    /// </summary>
    Invisible = 1u << 2,

    /// <summary>
    /// File needs to be backed up.
    /// </summary>
    NeedsBackup = 1u << 5,

    /// <summary>
    /// Rename enabled.
    /// </summary>
    RenameEnabled = 1u << 6,

    /// <summary>
    /// Destroy enabled.
    /// </summary>
    DestroyEnabled = 1u << 7,
}
