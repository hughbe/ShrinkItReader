namespace ShrinkItReader;

/// <summary>
/// The storage type identifiers used in ShrinkIt / NuFX headers.
/// </summary>
public enum ShrinkItStorageType
{
    /// <summary>
    /// Invalid storage type.
    /// </summary>
    Invalid = 0x00,

    /// <summary>
    /// Standard storage type 1.
    /// </summary>
    Standard1 = 0x01,

    /// <summary>
    /// Standard storage type 2.
    /// </summary>
    Standard2 = 0x02,

    /// <summary>
    /// Standard storage type 3.
    /// </summary>
    Standard3 = 0x03,

    /// <summary>
    /// GS/OS forked file.
    /// </summary>
    GSOSForkedFile = 0x05,

    /// <summary>
    /// Seedling (4K)
    /// </summary>
    Subdirectory = 0x0D,
}