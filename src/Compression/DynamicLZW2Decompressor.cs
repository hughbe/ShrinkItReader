namespace ShrinkItReader;

/// <summary>
/// Decompressor for Dynamic LZW/2 format used in ShrinkIt archives.
/// LZW/2 preserves the string table across 4K blocks for better compression ratios
/// and uses explicit clear codes (0x0100) to reset the table when needed.
/// </summary>
public class DynamicLzw2Decompressor
{
    /// <summary>
    /// Decompresses data from the provided stream using Dynamic LZW/2 algorithm.
    /// </summary>
    /// <param name="stream">The input stream containing compressed data.</param>
    /// <param name="compressedDataLength">The length of the compressed data.</param>
    /// <param name="decompressedDataLength">The length of the decompressed data.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] Decompress(Stream stream, long compressedDataLength, long decompressedDataLength)
    {
        return LzwDecompressor.Decompress(stream, compressedDataLength, decompressedDataLength, isType2: true);
    }
}
