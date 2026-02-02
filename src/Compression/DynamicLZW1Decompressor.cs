namespace ShrinkItReader;

/// <summary>
/// Decompressor for Dynamic LZW/1 format used in ShrinkIt archives.
/// LZW/1 resets the string table for each 4K block and includes a CRC-16 in the data stream.
/// </summary>
public static class DynamicLzw1Decompressor
{
    /// <summary>
    /// Decompresses data from the provided stream using Dynamic LZW/1 algorithm and writes directly to an output stream.
    /// </summary>
    /// <param name="inputStream">The input stream containing compressed data.</param>
    /// <param name="outputStream">The output stream to write decompressed data to.</param>
    /// <param name="compressedDataLength">The length of the compressed data.</param>
    /// <param name="decompressedDataLength">The length of the decompressed data.</param>
    public static void DecompressToStream(Stream inputStream, Stream outputStream, long compressedDataLength, long decompressedDataLength)
    {
        LzwDecompressor.DecompressToStream(inputStream, outputStream, compressedDataLength, decompressedDataLength, isType2: false);
    }
}
