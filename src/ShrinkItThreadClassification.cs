namespace ShrinkItReader;

/// <summary>
/// The thread classification identifiers used in ShrinkIt / NuFX headers.
/// </summary>
public enum ShrinkItThreadClassification : ushort
{
    /// <summary>
    /// Message thread.
    /// </summary>
    Message = 0x0000,

    /// <summary>
    /// Control thread.
    /// </summary>
    Control = 0x0001,

    /// <summary>
    /// Data thread.
    /// </summary>
    Data = 0x0002,

    /// <summary>
    /// File name thread.
    /// </summary>
    FileName = 0x0003,
}
