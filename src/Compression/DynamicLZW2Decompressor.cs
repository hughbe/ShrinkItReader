namespace ShrinkItReader;

/// <summary>
/// Decompressor for Dynamic LZW/2 format used in ShrinkIt archives.
/// LZW/2 preserves the string table across 4K blocks for better compression ratios
/// and uses explicit clear codes (0x0100) to reset the table when needed.
/// </summary>
public static class DynamicLzw2Decompressor
{
    /// <summary>
    /// Decompresses data from the provided stream using Dynamic LZW/2 algorithm and writes directly to an output stream.
    /// </summary>
    /// <param name="inputStream">The input stream containing compressed data.</param>
    /// <param name="outputStream">The output stream to write decompressed data to.</param>
    /// <param name="compressedDataLength">The length of the compressed data.</param>
    /// <param name="decompressedDataLength">The length of the decompressed data.</param>
    public static void DecompressToStream(Stream inputStream, Stream outputStream, long compressedDataLength, long decompressedDataLength)
    {
        LzwDecompressor.DecompressToStream(inputStream, outputStream, compressedDataLength, decompressedDataLength, isType2: true);
    }
}
