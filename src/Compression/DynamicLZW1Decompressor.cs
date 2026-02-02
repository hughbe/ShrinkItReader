namespace ShrinkItReader;

/// <summary>
/// Decompressor for Dynamic LZW/1 format used in ShrinkIt archives.
/// LZW/1 resets the string table for each 4K block and includes a CRC-16 in the data stream.
/// </summary>
public class DynamicLzw1Decompressor
{
    /// <summary>
    /// Decompresses data from the provided stream using Dynamic LZW/1 algorithm.
    /// </summary>
    /// <param name="stream">The input stream containing compressed data.</param>
    /// <param name="compressedDataLength">The length of the compressed data.</param>
    /// <param name="decompressedDataLength">The length of the decompressed data.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] Decompress(Stream stream, long compressedDataLength, long decompressedDataLength)
    {
        return LzwDecompressor.Decompress(stream, compressedDataLength, decompressedDataLength, isType2: false);
    }
}
