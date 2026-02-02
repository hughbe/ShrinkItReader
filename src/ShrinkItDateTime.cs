using System.Diagnostics;

namespace ShrinkItReader;

/// <summary>
/// A date and time structure used in ShrinkIt archives.
/// </summary>
public readonly struct ShrinkItDateTime
{
    /// <summary>
    /// The size of the structure in bytes.
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// Gets the seconds (0-59).
    /// </summary>
    public byte Second { get; }

    /// <summary>
    /// Gets the minutes (0-59).
    /// </summary>
    public byte Minute { get; }

    /// <summary>
    /// Gets the hours (0-23).
    /// </summary>
    public byte Hour { get; }

    /// <summary>
    /// Gets the year since 1900.
    /// </summary>
    public byte Year { get; }

    /// <summary>
    /// Gets the day (0-30).
    /// </summary>
    public byte Day { get; }

    /// <summary>
    /// Gets the month (0-11).
    /// </summary>
    public byte Month { get; }

    /// <summary>
    /// Gets the reserved filler byte (must be 0).
    /// </summary>
    public byte Filler { get; }

    /// <summary>
    /// Gets the weekday (1-7).
    /// </summary>
    public ShrinkItWeekday Weekday { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShrinkItDateTime"/> struct.
    /// </summary>
    /// <param name="data">The raw data for the ShrinkIt date and time.</param>
    /// <exception cref="ArgumentException">Thrown if the data is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any field is out of range.</exception>
    public ShrinkItDateTime(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be {Size} bytes in length.", nameof(data));
        }

        // Structure documented in https://nulib.com/library/FTN.e08002.htm
        int offset = 0;

        // The second, 0 through 59.
        Second = data[offset];
        offset += 1;

        if (Second > 59)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Seconds must be in the range 0-59.");
        }

        // The minute, 0 through 59.
        // Note: Some archives have values > 59, which overflow into hours.
        Minute = data[offset];
        offset += 1;

        // The hour, 0 through 23.
        // Note: Some archives have values > 23, which overflow into days.
        Hour = data[offset];
        offset += 1;

        // The current year minus 1900.
        Year = data[offset];
        offset += 1;

        // The day, 0 through 30.
        Day = data[offset];
        offset += 1;

        if (Day > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Day must be in the range 0-30.");
        }

        // The month, 0 through 11 (0 = January).
        Month = data[offset];
        offset += 1;

        if (Month > 11)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Month must be in the range 0-11.");
        }

        // Reserved, must be zero.
        Filler = data[offset];
        offset += 1;

        // The day of the week, 1 through 7 (1 = Sunday).
        Weekday = (ShrinkItWeekday)data[offset];
        offset += 1;

        if (!Enum.IsDefined(Weekday))
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Weekday must be in the range 1-7.");
        }

        Debug.Assert(offset == data.Length, "Did not consume all bytes for ShrinkItDateTime.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public DateTime ToDateTime()
    {
        var year = Year + 1900;
        if (year < 1940)
        {
            year += 100;
        }

        // Handle overflow: minutes > 59 should be added to hours
        var minute = Minute % 60;
        var hour = Hour + (Minute / 60);

        // Handle overflow: hours > 23 should be added to days
        var extraDays = hour / 24;
        hour %= 24;

        return new DateTime(year, Month + 1, Day + 1, hour, minute, Second).AddDays(extraDays);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dt"></param>
    public static implicit operator DateTime(ShrinkItDateTime dt) => dt.ToDateTime();

    /// <inheritdoc/>
    public override string ToString() => ToDateTime().ToString();
}
