using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace PostHog.Library;

/// <summary>
/// Represents a date that can be compared to other dates. This might be a relative date or a standard date.
/// </summary>
public partial record RelativeDate
{
    static readonly Regex RelativeDateRegex = MyRegex();
    readonly string _value;
    readonly Func<DateTimeOffset, DateTimeOffset?>? _currentDateOffsetFunc;

    private RelativeDate(string value, Func<DateTimeOffset, DateTimeOffset?> currentDateOffsetFunc)
    {
        _value = value;
        _currentDateOffsetFunc = currentDateOffsetFunc;
    }

    /// <summary>
    /// Determines whether the specified date is before the date represented by this instance.
    /// </summary>
    /// <param name="other">The date to compare to this instance.</param>
    /// <param name="now">The current date.</param>
    public bool IsDateBefore(DateTimeOffset other, DateTimeOffset now) => other < _currentDateOffsetFunc?.Invoke(now);

    /// <summary>
    /// Determines whether the specified date is after the date represented by this instance.
    /// </summary>
    /// <remarks>
    /// The astute code reviewer will note that this method actually tests if the date is on or after the specified
    /// date. In practice, this doesn't matter because the dates we use have sub-second precision and the likelihood of
    /// the two dates being exactly the same is miniscule. Yet, here I am handling it.
    /// </remarks>
    /// <param name="other">The date to compare to this instance.</param>
    /// <param name="now">The current date.</param>
    public bool IsDateAfter(DateTimeOffset other, DateTimeOffset now) => !IsDateBefore(other, now);

    /// <summary>
    /// Determines whether the specified date is before the date represented by this instance.
    /// </summary>
    /// <param name="other">The date to compare to this instance.</param>
    /// <param name="now">The current date.</param>
    public bool IsDateBefore(DateTime other, DateTimeOffset now) => other < _currentDateOffsetFunc?.Invoke(now);

    /// <summary>
    /// Determines whether the specified date is after the date represented by this instance.
    /// </summary>
    /// <param name="other">The date to compare to this instance.</param>
    /// <param name="now">The current date.</param>
    public bool IsDateAfter(DateTime other, DateTimeOffset now) => !IsDateBefore(other, now);

    /// <summary>
    /// Tries to parse a string into a <see cref="RelativeDate"/>. If parsing fails, returns <c>null</c>.
    /// </summary>
    /// <param name="value">The relative or absolute date string.</param>
    /// <returns><c>true</c> if parsing was successful. Otherwise <c>false</c>.</returns>
    public static RelativeDate? Parse(string value)
        => TryParseRelativeDate(value, out var relativeDate) ? relativeDate : null;

    /// <summary>
    /// Tries to parse a string into a <see cref="RelativeDate"/>.
    /// </summary>
    /// <param name="value">The relative or absolute date string.</param>
    /// <param name="relativeDate">The resulting <see cref="RelativeDate"/>.</param>
    /// <returns><c>true</c> if parsing was successful. Otherwise <c>false</c>.</returns>
    public static bool TryParseRelativeDate(
        string? value,
        [NotNullWhen(returnValue: true)] out RelativeDate? relativeDate)
    {
        if (value is null)
        {
            relativeDate = null;
            return false;
        }
        var match = RelativeDateRegex.Match(value);
        if (match.Success)
        {
            if (int.TryParse(match.Groups["number"].Value, out int number))
            {
                var unit = match.Groups["unit"].Value;

                Func<DateTimeOffset, DateTimeOffset?> func = unit switch
                {
                    "h" => now => now.AddHours(-number),
                    "d" => now => now.AddDays(-number),
                    "w" => now => now.AddDays(-number * 7),
                    "m" => now => now.AddMonths(-number),
                    "y" => now => now.AddYears(-number),
                    _ => _ => null
                };
                relativeDate = new RelativeDate(value, func);
                return true;
            }
        }

        relativeDate = null;
        return false;
    }

    [GeneratedRegex(pattern: @"^-(?<number>\d+)(?<unit>[hdwmy])$", options: RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}