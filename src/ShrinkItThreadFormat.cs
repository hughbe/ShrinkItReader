namespace ShrinkItReader;

/// <summary>
/// The thread format identifiers used in ShrinkIt / NuFX headers.
/// </summary>
public enum ShrinkItThreadFormat : ushort
{
    /// <summary>
    /// Uncompressed format.
    /// </summary>
    Uncompressed = 0x0000,

    /// <summary>
    /// Huffman Squeeze format.
    /// </summary>
    HuffmanSqueeze = 0x0001,

    /// <summary>
    /// Dynamic LZW format 1 (ShrinkIt specific).
    /// </summary>
    DynamicLzw1 = 0x0002,

    /// <summary>
    /// Dynamic LZW format 2 (ShrinkIt specific).
    /// </summary>
    DynamicLzw2 = 0x0003,

    /// <summary>
    /// Unix 12-bit compress format.
    /// </summary>
    Unix12BitCompress = 0x0004,

    /// <summary>
    /// Unix 16-bit compress format.
    /// </summary>
    Unix16BitCompress = 0x0005,
}