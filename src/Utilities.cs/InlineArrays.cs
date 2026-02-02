using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ShrinkItReader.Utilities;

/// <summary>
/// An inline array of 4 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray4
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 4;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray4"/> struct.
    /// </summary>
    public ByteArray4(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);
}

/// <summary>
/// An inline array of 6 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray6
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 6;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray6"/> struct.
    /// </summary>
    public ByteArray6(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);
}

/// <summary>
/// An inline array of 8 bytes.
/// </summary>
[InlineArray(Size)]
public struct ByteArray8
{
    /// <summary>
    /// The size of the array in bytes.
    /// </summary>
    public const int Size = 8;

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArray8"/> struct.
    /// </summary>
    public ByteArray8(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);
}
