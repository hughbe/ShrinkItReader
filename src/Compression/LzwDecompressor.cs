using System.Buffers;
using System.Buffers.Binary;
using ShrinkItReader.Utilities;

namespace ShrinkItReader;

/// <summary>
/// Shared LZW/RLE decompression implementation for Dynamic LZW/1 and LZW/2 formats.
/// Based on the NuFX specification (FTN.e08002) and the CiderPress nufxlib reference implementation.
/// </summary>
internal static class LzwDecompressor
{
    private const int BlockSize = 4096;
    private const int ClearCode = 0x0100;
    private const int FirstCode = 0x0101;
    private const int MaxTrieSize = 4096 - 256;

    /// <summary>
    /// Mask table indexed by <c>(entry + 1) &gt;&gt; 8</c>, used to extract variable-width codes.
    /// </summary>
    private static ReadOnlySpan<uint> MaskTable =>
    [
        0x0000, 0x01ff, 0x03ff, 0x03ff, 0x07ff, 0x07ff, 0x07ff, 0x07ff,
        0x0fff, 0x0fff, 0x0fff, 0x0fff, 0x0fff, 0x0fff, 0x0fff, 0x0fff,
        0x0fff
    ];

    /// <summary>
    /// Bit width table indexed by <c>(entry + 1) &gt;&gt; 8</c>, gives the number of bits per code.
    /// </summary>
    private static ReadOnlySpan<int> BitWidthTable =>
    [
        8, 9, 10, 10, 11, 11, 11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12
    ];

    /// <summary>
    /// Decompresses LZW/1 or LZW/2 compressed data from a stream and writes the output directly to the destination stream.
    /// </summary>
    /// <param name="inputStream">The input stream containing compressed data.</param>
    /// <param name="outputStream">The output stream to write decompressed data to.</param>
    /// <param name="compressedDataLength">The length of the compressed data.</param>
    /// <param name="decompressedDataLength">The expected length of the decompressed data.</param>
    /// <param name="isType2">True for LZW/2 format, false for LZW/1 format.</param>
    public static void DecompressToStream(Stream inputStream, Stream outputStream, long compressedDataLength, long decompressedDataLength, bool isType2)
    {
        byte[] compressed = ArrayPool<byte>.Shared.Rent((int)compressedDataLength);
        try
        {
            ReadExact(inputStream, compressed, (int)compressedDataLength);
            DecompressCoreToStream(compressed, (int)compressedDataLength, (int)decompressedDataLength, isType2, outputStream);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(compressed);
        }
    }

    private static void ReadExact(Stream stream, byte[] buffer, int length)
    {
        int totalRead = 0;
        while (totalRead < length)
        {
            int read = stream.Read(buffer, totalRead, length - totalRead);
            if (read == 0)
                throw new InvalidOperationException("Unexpected end of compressed data stream.");
            totalRead += read;
        }
    }

    /// <summary>
    /// Core decompression implementation that writes output directly to a stream.
    /// </summary>
    private static void DecompressCoreToStream(byte[] compressed, int compressedLength, int decompressedDataLength, bool isType2, Stream outputStream)
    {
        int pos = 0;

        // LZW/1 has a 2-byte CRC at the start of the stream
        ushort fileCrc = 0;
        if (!isType2)
        {
            fileCrc = BinaryPrimitives.ReadUInt16LittleEndian(compressed.AsSpan(pos, 2));
            pos += 2;
        }

        // Both formats have volume number and RLE escape character
        byte diskVol = compressed[pos++];
        byte rleEscape = compressed[pos++];

        int uncompRemaining = decompressedDataLength;

        // Rent reusable buffers from the pool
        byte[] trieCh = ArrayPool<byte>.Shared.Rent(MaxTrieSize);
        uint[] triePrefix = ArrayPool<uint>.Shared.Rent(MaxTrieSize);
        byte[] lzwBuf = ArrayPool<byte>.Shared.Rent(BlockSize + 64);
        byte[] rleBuf = ArrayPool<byte>.Shared.Rent(BlockSize);
        byte[] stack = ArrayPool<byte>.Shared.Rent(BlockSize);

        try
        {
            // LZW/2 persistent state across blocks
            int entry = FirstCode;
            uint oldcode = 0, incode = 0, finalc = 0;
            bool resetFix = false;

            // LZW/1 CRC accumulator
            ushort chunkCrc = 0x0000;

            while (uncompRemaining > 0)
            {
                // Parse per-block header
                int rleLen;
                bool lzwUsed;
                int lzwLen = -1;

                if (isType2)
                {
                    int raw = compressed[pos] | (compressed[pos + 1] << 8);
                    pos += 2;
                    lzwUsed = (raw & 0x8000) != 0;
                    rleLen = raw & 0x1fff;

                    if (lzwUsed)
                    {
                        lzwLen = compressed[pos] | (compressed[pos + 1] << 8);
                        pos += 2;
                        lzwLen -= 4; // exclude the 4 header bytes
                    }
                }
                else
                {
                    rleLen = compressed[pos] | (compressed[pos + 1] << 8);
                    pos += 2;
                    byte lzwFlag = compressed[pos++];
                    if (lzwFlag > 1)
                        throw new InvalidOperationException("Garbled LZW/1 block header.");
                    lzwUsed = lzwFlag != 0;
                }

                bool rleUsed = rleLen != BlockSize;
                int writeLen = Math.Min(BlockSize, uncompRemaining);

                // Pointer to the block data to copy to output
                byte[] blockBuf;
                int blockOffset = 0;

                if (lzwUsed)
                {
                    // Clear the lzwBuf using Span for better performance
                    lzwBuf.AsSpan(0, BlockSize).Clear();

                    if (!isType2)
                    {
                        // LZW/1: table is cleared for each block
                        int consumed = ExpandLzw1(compressed.AsSpan(pos), lzwBuf, rleLen, trieCh, triePrefix, stack);
                        pos += consumed;
                    }
                    else
                    {
                        // LZW/2: table persists across blocks
                        int consumed = ExpandLzw2(compressed.AsSpan(pos), lzwBuf, rleLen,
                            trieCh, triePrefix, stack,
                            ref entry, ref oldcode, ref incode, ref finalc, ref resetFix,
                            lzwLen);
                        pos += consumed;
                    }

                    if (rleUsed)
                    {
                        rleBuf.AsSpan(0, BlockSize).Clear();
                        ExpandRle(lzwBuf.AsSpan(0, rleLen), rleBuf, rleEscape);
                        blockBuf = rleBuf;
                    }
                    else
                    {
                        blockBuf = lzwBuf;
                    }
                }
                else
                {
                    if (rleUsed)
                    {
                        rleBuf.AsSpan(0, BlockSize).Clear();
                        ExpandRle(compressed.AsSpan(pos, rleLen), rleBuf, rleEscape);
                        pos += rleLen;
                        blockBuf = rleBuf;
                    }
                    else
                    {
                        // No compression at all - raw 4K block, use compressed buffer directly
                        blockBuf = compressed;
                        blockOffset = pos;
                        pos += writeLen;
                    }

                    // When LZW is not used, reset LZW/2 table state
                    if (isType2)
                    {
                        entry = FirstCode;
                        resetFix = false;
                    }
                }

                // CRC for LZW/1 always covers the full 4K block (including zero-padding)
                if (!isType2)
                {
                    chunkCrc = Crc16.Calculate(blockBuf.AsSpan(blockOffset, BlockSize), chunkCrc);
                }

                // Write directly to output stream instead of copying to an intermediate buffer
                outputStream.Write(blockBuf, blockOffset, writeLen);
                uncompRemaining -= writeLen;
            }

            // Verify LZW/1 embedded CRC
            if (!isType2 && chunkCrc != fileCrc)
            {
                throw new InvalidOperationException(
                    $"LZW/1 CRC mismatch: expected 0x{fileCrc:X4}, got 0x{chunkCrc:X4}.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(trieCh);
            ArrayPool<uint>.Shared.Return(triePrefix);
            ArrayPool<byte>.Shared.Return(lzwBuf);
            ArrayPool<byte>.Shared.Return(rleBuf);
            ArrayPool<byte>.Shared.Return(stack);
        }
    }

    /// <summary>
    /// Reads a variable-width LZW code from the input bitstream.
    /// The bit width is determined by the current table entry count.
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static uint GetCode(ReadOnlySpan<byte> input, ref int pos, int entry, ref int atBit, ref uint lastByte)
    {
        int numBits = (entry + 1) >> 8;
        int startBit = atBit;
        int lastBitPos = startBit + BitWidthTable[numBits];

        uint value = startBit == 0 ? input[pos++] : lastByte;

        if (lastBitPos > 16)
        {
            // Need two more bytes from input
            value |= (uint)input[pos++] << 8;
            lastByte = input[pos++];
            value |= lastByte << 16;
        }
        else
        {
            // Need one more byte from input
            lastByte = input[pos++];
            value |= lastByte << 8;
        }

        atBit = lastBitPos & 0x07;
        return (value >> startBit) & MaskTable[numBits];
    }

    /// <summary>
    /// Expands a single LZW/1 block. The string table is reset for each block.
    /// </summary>
    /// <returns>Number of input bytes consumed.</returns>
    private static int ExpandLzw1(ReadOnlySpan<byte> input, Span<byte> output, int expectedLen,
        byte[] trieCh, uint[] triePrefix, byte[] stack)
    {
        int inPos = 0;
        int outPos = 0;
        int atBit = 0;
        uint lastByte = 0;

        int entry = FirstCode;

        // Read and output first code (must be a literal byte value)
        uint code = GetCode(input, ref inPos, entry, ref atBit, ref lastByte);
        if (code > 0xff)
            throw new InvalidOperationException("Invalid initial LZW/1 symbol.");

        uint finalc = code;
        uint oldcode = code;
        output[outPos++] = (byte)code;

        int stackPos = 0;

        while (outPos < expectedLen)
        {
            uint incode = GetCode(input, ref inPos, entry, ref atBit, ref lastByte);
            uint ptr = incode;

            // Handle KwKwK special case
            if (ptr >= (uint)entry)
            {
                if (ptr != (uint)entry)
                    throw new InvalidOperationException("Bad LZW/1 code.");
                stack[stackPos++] = (byte)finalc;
                ptr = oldcode;
            }

            // Chase up the trie to build the output string
            while (ptr > 0xff)
            {
                int idx = (int)ptr - 256;
                stack[stackPos++] = trieCh[idx];
                ptr = triePrefix[idx];
            }

            // Output first character of the string
            finalc = ptr;
            output[outPos++] = (byte)ptr;

            // Output rest of string from stack (reverse order)
            while (stackPos > 0)
                output[outPos++] = stack[--stackPos];

            // Add new entry to the trie
            int newIdx = entry - 256;
            trieCh[newIdx] = (byte)finalc;
            triePrefix[newIdx] = oldcode;
            entry++;
            oldcode = incode;
        }

        return inPos;
    }

    /// <summary>
    /// Expands a single LZW/2 block. The string table persists across blocks
    /// and is cleared explicitly via clear codes (0x0100).
    /// </summary>
    /// <returns>Number of input bytes consumed.</returns>
    private static int ExpandLzw2(ReadOnlySpan<byte> input, Span<byte> output, int expectedLen,
        byte[] trieCh, uint[] triePrefix, byte[] stack,
        ref int entry, ref uint oldcode, ref uint incode, ref uint finalc, ref bool resetFix,
        int expectedInputUsed)
    {
        int inPos = 0;
        int outPos = 0;
        int atBit = 0;
        uint lastByte = 0;

        int stackPos = 0;

        // Determine if we need to initialize the table
        bool needClearTable = (entry == FirstCode && !resetFix);
        if (!needClearTable)
            resetFix = false;

        while (outPos < expectedLen || needClearTable)
        {
            if (needClearTable)
            {
                entry = FirstCode;
                needClearTable = false;

                if (outPos >= expectedLen)
                    break;

                // Read and output the first code after a table clear
                finalc = oldcode = incode = GetCode(input, ref inPos, entry, ref atBit, ref lastByte);
                if (incode > 0xff)
                    throw new InvalidOperationException("Invalid initial LZW/2 symbol.");
                output[outPos++] = (byte)incode;

                if (outPos >= expectedLen)
                {
                    // Table clear was second-to-last code; flag for next block
                    resetFix = true;
                    break;
                }

                continue;
            }

            incode = GetCode(input, ref inPos, entry, ref atBit, ref lastByte);
            uint ptr = incode;

            if (incode == ClearCode)
            {
                needClearTable = true;
                continue;
            }

            // Handle KwKwK special case
            if (ptr >= (uint)entry)
            {
                if (ptr != (uint)entry)
                    throw new InvalidOperationException("Bad LZW/2 code.");
                stack[stackPos++] = (byte)finalc;
                ptr = oldcode;
            }

            // Chase up the trie
            while (ptr > 0xff)
            {
                int idx = (int)ptr - 256;
                stack[stackPos++] = trieCh[idx];
                ptr = triePrefix[idx];
            }

            // Output first character
            finalc = ptr;
            output[outPos++] = (byte)ptr;

            // Output rest from stack
            while (stackPos > 0)
                output[outPos++] = stack[--stackPos];

            // Add new entry to trie
            int newIdx = entry - 256;
            trieCh[newIdx] = (byte)finalc;
            triePrefix[newIdx] = oldcode;
            entry++;
            oldcode = incode;
        }

        // Verify input consumption when expected length is known
        if (expectedInputUsed >= 0 && inPos != expectedInputUsed)
        {
            throw new InvalidOperationException(
                $"LZW/2 input length mismatch: consumed {inPos}, expected {expectedInputUsed}.");
        }

        return inPos;
    }

    /// <summary>
    /// Expands RLE-compressed data into a 4K block.
    /// Format: escape byte followed by character and count (count is zero-based, so count+1 repetitions).
    /// </summary>
    private static void ExpandRle(ReadOnlySpan<byte> input, Span<byte> output, byte rleEscape)
    {
        int inPos = 0;
        int outPos = 0;

        while (outPos < BlockSize)
        {
            byte uch = input[inPos++];
            if (uch == rleEscape)
            {
                byte ch = input[inPos++];
                int count = input[inPos++];
                // Don't overrun output buffer
                int remaining = BlockSize - outPos;
                int toWrite = count + 1; // count is zero-based
                if (toWrite >= remaining)
                {
                    // Fill remaining space using Span.Fill for efficiency
                    output.Slice(outPos, remaining).Fill(ch);
                    break;
                }
                // count+1 repetitions using Span.Fill
                output.Slice(outPos, toWrite).Fill(ch);
                outPos += toWrite;
            }
            else
            {
                output[outPos++] = uch;
            }
        }
    }
}
