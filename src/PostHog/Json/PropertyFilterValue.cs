using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PostHog.Api;
using PostHog.Library;
using static PostHog.Library.Ensure;

namespace PostHog.Json;

/// <summary>
/// Represents a filter property value (<see cref="PropertyFilter"/>). This is the value that is used to compare against
/// the value of a property in a user or group, often called the "override value".
/// </summary>
/// <remarks>
/// The supported types are limited to the types we store in filter property values.
/// </remarks>
[JsonConverter(typeof(PropertyFilterValueJsonConverter))]
public class PropertyFilterValue
{
    /// <summary>
    /// If this value is a boolean, this property will be set.
    /// </summary>
    public bool? BoolValue { get; }

    /// <summary>
    /// If this value is a double, this property will be set.
    /// </summary>
    public double? DoubleValue { get; }

    /// <summary>
    /// If this value is an integer, this property will be set.
    /// </summary>
    public long? IntegerValue { get; }

    /// <summary>
    /// If this value is a string, this property will be set.
    /// </summary>
    public string? StringValue { get; }

    /// <summary>
    /// If this value is an array of strings, this property will be set.
    /// </summary>
    public IReadOnlyList<string>? ListOfStrings { get; }

    /// <summary>
    /// If this value is an array of integers, this property will be set.
    /// </summary>
    public IReadOnlyList<long>? ListOfIntegers { get; }

    /// <summary>
    /// If this value is an array of doubles (floats), this property will be set.
    /// </summary>
    public IReadOnlyList<double>? ListOfDoubles { get; }

    /// <summary>
    /// Creates a new instance of <see cref="PropertyFilterValue"/> from the specified <paramref name="jsonElement"/>.
    /// </summary>
    /// <param name="jsonElement">A JsonElement</param>
    /// <returns>A <see cref="PropertyFilterValue"/>.</returns>
    public static PropertyFilterValue? Create(JsonElement jsonElement) =>
        jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString() is { } stringValue ? new PropertyFilterValue(stringValue) : null,
            JsonValueKind.Array when TryParseIntArray(jsonElement, out var integerArrayValue)
                => new PropertyFilterValue(integerArrayValue),
            JsonValueKind.Array when TryParseDoubleArray(jsonElement, out var doubleArrayValue)
                => new PropertyFilterValue(doubleArrayValue),
            JsonValueKind.Array when TryParseStringArray(jsonElement, out var stringArrayValue)
                => new PropertyFilterValue(stringArrayValue),
            JsonValueKind.Number => jsonElement.TryGetInt64(out var integerValue)
                ? new PropertyFilterValue(integerValue)
                : jsonElement.TryGetDouble(out var doubleValue)
                    ? new PropertyFilterValue(doubleValue)
                    : new PropertyFilterValue(jsonElement.GetString() ?? string.Empty),
            JsonValueKind.True => new PropertyFilterValue(true),
            JsonValueKind.False => new PropertyFilterValue(false),
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"JsonValueKind: {jsonElement.ValueKind} is not supported for filter property values.", nameof(jsonElement))
        };

    public PropertyFilterValue(bool? boolValue)
    {
        BoolValue = boolValue;
    }

    public PropertyFilterValue(IReadOnlyList<string> listOfStrings)
    {
        ListOfStrings = listOfStrings;
    }

    public PropertyFilterValue(IReadOnlyList<long> listOfIntegers)
    {
        ListOfIntegers = listOfIntegers;
    }

    public PropertyFilterValue(IReadOnlyList<double> listOfDoubles)
    {
        ListOfDoubles = listOfDoubles;
    }

    public PropertyFilterValue(long integerValue)
    {
        IntegerValue = integerValue;
    }

    public PropertyFilterValue(double doubleValue)
    {
        DoubleValue = doubleValue;
    }

    public PropertyFilterValue(string stringValue)
    {
        StringValue = stringValue;
    }

    /// <summary>
    /// Does a regular expression match on this instance with the specified <paramref name="input"/> instance.
    /// </summary>
    /// <param name="input">The value to search with a regex. For non-strings, we'll call ToString and run the regex.</param>
    /// <returns><c>true</c>If the current value is a valid regex and it matches the other value.</returns>
    public bool IsRegexMatch(object? input)
    {
        if (input is null)
        {
            return false;
        }

        if (StringValue is null || !RegexHelpers.TryValidateRegex(StringValue, out var regex, RegexOptions.None))
        {
            return false;
        }

        return regex.IsMatch(NotNull(input.ToString()));
    }

    /// <summary>
    /// Returns a value indicating whether this instance contains the specified <paramref name="other"/> instance.
    /// </summary>
    /// <param name="other">The other value to compare to this one.</param>
    /// <param name="stringComparison">The type of comparison if these are strings.</param>
    /// <returns><c>true</c> if this instance contains the other.</returns>
    public bool Contains(object? other, StringComparison stringComparison) =>
        other?.ToString() is { } comparandString
        && StringValue is not null
        && StringValue.Contains(comparandString, stringComparison);

    /// <summary>
    /// Determines whether the specified <paramref name="overrideValue"/> is an "exact" match for this instance.
    /// If this instance is an array, then it's checking to see if the value is in the array.
    /// </summary>
    /// <param name="overrideValue">The override value.</param>
    /// <returns><c>true</c> if the override value is an "exact" match for this value.</returns>
    public bool IsExactMatch(object? overrideValue)
    {
        return overrideValue switch
        {
            int overrideIntValue when ListOfIntegers is not null => ListOfIntegers.Contains(overrideIntValue),
            long overrideLongValue when ListOfIntegers is not null => ListOfIntegers.Contains(overrideLongValue),
            double overrideDoubleValue when ListOfIntegers is not null => ListOfIntegers.Select(i => (double)i).Contains(overrideDoubleValue),
            double overrideDoubleValue when ListOfDoubles is not null => ListOfDoubles.Contains(overrideDoubleValue),
            int overrideIntValue when ListOfDoubles is not null => ListOfDoubles.Contains(overrideIntValue),
            long overrideLongValue when ListOfDoubles is not null => ListOfDoubles.Contains(overrideLongValue),
            string overrideStringValue when ListOfStrings is not null => ListOfStrings.Contains(overrideStringValue, StringComparer.OrdinalIgnoreCase),
            _ => CompareTo(overrideValue) == 0 // Defer to CompareTo for all other types.
        };
    }


    /// <summary>
    /// Compares this instance with the specified <paramref name="other"/> instance and indicates whether this instance
    /// precedes, follows, or appears in the same position in the sort order as the specified instance.
    /// Less than zero: This instance precedes <paramref name="other"/> in the sort order.
    /// Zero: This instance appears in the same position in the sort order as <paramref name="other"/>.
    /// Greater than zero: This instance follows <paramref name="other"/> in the sort order or other is null.
    /// </summary>
    /// <remarks>
    /// For string values, does a case-insensitive comparison.
    /// </remarks>
    /// <param name="other">The <see cref="PropertyFilterValue"/> to compare with.</param>
    /// <returns>
    /// A value that indicates the relative order of the objects being compared. The return value has these meanings:
    /// 0: This instance and <paramref name="other"/> are equal.
    /// -1: This instance precedes <paramref name="other"/> in the sort order.
    /// 1: This instance follows <paramref name="other"/> in the sort order.
    /// </returns>
    public int CompareTo(object? other)
    {
        if (ReferenceEquals(other, null))
        {
            return 1;
        }

        return this switch
        {
            { StringValue: { } stringValue } when other is DateTime => string.Compare(stringValue, other.ToString(), StringComparison.OrdinalIgnoreCase),
            { StringValue: { } stringValue } when other is DateTimeOffset => string.Compare(stringValue, other.ToString(), StringComparison.OrdinalIgnoreCase),
            { StringValue: { } stringValue } => string.Compare(stringValue, other.ToString(), StringComparison.OrdinalIgnoreCase),
            { DoubleValue: { } doubleValue } when other is double overrideDouble => doubleValue.CompareTo(overrideDouble),
            { DoubleValue: { } doubleValue } when other is long overrideLong => doubleValue.CompareTo(overrideLong),
            { DoubleValue: { } doubleValue } when other is int overrideInt => doubleValue.CompareTo(overrideInt),
            { IntegerValue: { } longValue } when other is long => longValue.CompareTo(other),
            { IntegerValue: { } longValue } when other is int otherInt32 => longValue.CompareTo(Convert.ToInt64(otherInt32)),
            { IntegerValue: { } longValue } when other is double otherDouble => ((double)longValue).CompareTo(otherDouble),
            { BoolValue: { } boolValue } => boolValue.CompareTo(other),
            _ => 1
        };
    }

    /// <summary>
    /// Determines whether the override value represents a date greater than the date represented by this instance.
    /// </summary>
    /// <param name="overrideValue">The supplied override value.</param>
    /// <param name="now">The current date.</param>
    /// <returns><c>true</c> if the override date value is before the date represented by the filter value.</returns>
    /// <exception cref="InconclusiveMatchException">Thrown if the filter value can't be parsed.</exception>
    public bool IsDateBefore(object? overrideValue, DateTimeOffset now)
    {
        // Question: Should we support DateOnly and TimeOnly?
        if (overrideValue is not (string or DateTimeOffset or DateTime))
        {
            throw new InconclusiveMatchException("The date provided must be a string, DateTime, or DateTimeOffset, object");
        }

        if (!RelativeDate.TryParseRelativeDate(StringValue, out var relativeDate))
        {
            throw new InconclusiveMatchException("The date set on the flag is not a valid format.");
        }

        if (overrideValue is string overrideValueString)
        {
            overrideValue = DateTimeOffset.TryParse(overrideValueString, out var dateTimeOffset)
                ? dateTimeOffset
                : DateTime.TryParse(overrideValueString, out var dateTime)
                    ? dateTime
                    : throw new InconclusiveMatchException("The date provided is not a valid format");
        }

        return overrideValue is DateTimeOffset overrideDateTimeOffset && relativeDate.IsDateBefore(overrideDateTimeOffset, now)
               || (overrideValue is DateTime overrideDateTime && relativeDate.IsDateBefore(overrideDateTime, now));
    }

    public static bool operator >(PropertyFilterValue left, object? right) => NotNull(left).CompareTo(right) > 0;
    public static bool operator <(PropertyFilterValue? left, object? right) => NotNull(left).CompareTo(left) < 0;
    public static bool operator >=(PropertyFilterValue left, object? right) => NotNull(left).CompareTo(right) >= 0;
    public static bool operator <=(PropertyFilterValue left, object? right) => NotNull(left).CompareTo(right) <= 0;

    public override string ToString()
    {
        return this switch
        {
            { StringValue: { } stringValue } => stringValue,
            { ListOfIntegers: { } intArrayValue } => string.Join(", ", intArrayValue),
            { ListOfDoubles: { } doubleArrayValue } => string.Join(", ", doubleArrayValue),
            { DoubleValue: { } doubleValue } => doubleValue.ToString(CultureInfo.InvariantCulture),
            { IntegerValue: { } intValue } => intValue.ToString(CultureInfo.InvariantCulture),
            { BoolValue: { } boolValue } => boolValue.ToString(),
            _ => string.Empty
        };
    }

    public override bool Equals(object? obj) =>
        obj is PropertyFilterValue other
        && Equals(other);

    public override int GetHashCode() => HashCode.Combine(BoolValue, DoubleValue, IntegerValue, StringValue, ListOfStrings, ListOfIntegers, ListOfDoubles);

    /// <summary>
    /// Determines if this instance is equal to the specified <paramref name="other"/> <see cref="PropertyFilterValue"/>
    /// instance. This should not be used when evaluating filter property conditions.
    /// </summary>
    /// <param name="other">The <see cref="PropertyFilterValue"/> to compare to.</param>
    /// <returns><c>true</c> if these represent the same filter property value.</returns>
    public bool Equals(PropertyFilterValue? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ListOfStrings.ListsAreEqual(other.ListOfStrings)
               && ListOfDoubles.ListsAreEqual(other.ListOfDoubles)
               && ListOfIntegers.ListsAreEqual(other.ListOfIntegers)
               && ListOfDoubles.ListsAreEqual(other.ListOfDoubles)
               && StringValue == other.StringValue
               && IntegerValue == other.IntegerValue
               && DoubleValue == other.DoubleValue
               && BoolValue == other.BoolValue;
    }

    static bool TryParseStringArray(
        JsonElement jsonElement,
        [NotNullWhen(returnValue: true)] out IReadOnlyList<string>? value)
    {
        List<string> values = new();
        foreach (var element in jsonElement.EnumerateArray())
        {
            if (element.ValueKind is not JsonValueKind.String)
            {
                value = null;
                return false;
            }
            if (element.GetString() is { } stringValue)
            {
                values.Add(stringValue);
            }
        }

        value = values.ToReadOnlyList();
        return true;
    }

    static bool TryParseDoubleArray(
        JsonElement jsonElement,
        [NotNullWhen(returnValue: true)] out IReadOnlyList<double>? value)
    {
        List<double> values = new();
        foreach (var element in jsonElement.EnumerateArray())
        {
            if (element.ValueKind is not JsonValueKind.Number || !element.TryGetDouble(out var doubleValue))
            {
                value = null;
                return false;
            }
            values.Add(doubleValue);
        }

        value = values.ToReadOnlyList();
        return true;
    }

    static bool TryParseIntArray(
        JsonElement jsonElement,
        [NotNullWhen(returnValue: true)] out IReadOnlyList<long>? value)
    {
        List<long> values = new();
        foreach (var element in jsonElement.EnumerateArray())
        {
            if (element.ValueKind is not JsonValueKind.Number || !element.TryGetInt64(out var intValue))
            {
                value = null;
                return false;
            }
            values.Add(intValue);
        }

        value = values.ToReadOnlyList();
        return true;
    }
}