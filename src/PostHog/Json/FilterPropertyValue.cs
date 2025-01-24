using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PostHog.Api;
using PostHog.Library;
using static PostHog.Library.Ensure;

namespace PostHog.Json;

/// <summary>
/// Represents a filter property value (<see cref="FilterProperty"/>). This is the value that is used to compare against
/// the value of a property in a user or group, often called the "override value".
/// </summary>
/// <remarks>
/// The supported types are limited to the types we store in filter property values.
/// </remarks>
public class FilterPropertyValue
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
    public int? IntValue { get; }

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
    public IReadOnlyList<int>? ListOfInts { get; }

    /// <summary>
    /// If this value is an array of doubles (floats), this property will be set.
    /// </summary>
    public IReadOnlyList<double>? ListOfDoubles { get; }

    readonly JsonElement _sourceJsonElement;

    /// <summary>
    /// Creates a new instance of <see cref="FilterPropertyValue"/> from the specified <paramref name="jsonElement"/>.
    /// </summary>
    /// <param name="jsonElement">A JsonElement</param>
    /// <returns>A <see cref="FilterPropertyValue"/>.</returns>
    public static FilterPropertyValue? Create(JsonElement jsonElement) =>
        jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString() is { } stringValue ? new FilterPropertyValue(stringValue, jsonElement) : null,
            JsonValueKind.Array when TryParseIntArray(jsonElement, out var intArrayValue)
                => new FilterPropertyValue(intArrayValue, jsonElement),
            JsonValueKind.Array when TryParseDoubleArray(jsonElement, out var doubleArrayValue)
                => new FilterPropertyValue(doubleArrayValue, jsonElement),
            JsonValueKind.Array when TryParseStringArray(jsonElement, out var stringArrayValue)
                => new FilterPropertyValue(stringArrayValue, jsonElement),
            JsonValueKind.Number => jsonElement.TryGetDouble(out var doubleValue)
                ? new FilterPropertyValue(doubleValue, jsonElement)
                : new FilterPropertyValue(jsonElement.GetInt32(), jsonElement),
            JsonValueKind.True => new FilterPropertyValue(true, jsonElement),
            JsonValueKind.False => new FilterPropertyValue(false, jsonElement),
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"JsonValueKind: {jsonElement.ValueKind} is not supported for filter property values.", nameof(jsonElement))
        };

    FilterPropertyValue(bool? boolValue, JsonElement sourceJsonElement)
    {
        BoolValue = boolValue;
        _sourceJsonElement = sourceJsonElement;
    }

    FilterPropertyValue(IReadOnlyList<string> listOfStrings, JsonElement sourceJsonElement)
    {
        ListOfStrings = listOfStrings;
        _sourceJsonElement = sourceJsonElement;
    }

    FilterPropertyValue(IReadOnlyList<int> listOfInts, JsonElement sourceJsonElement)
    {
        ListOfInts = listOfInts;
        _sourceJsonElement = sourceJsonElement;
    }

    FilterPropertyValue(int intValue, JsonElement sourceJsonElement)
    {
        IntValue = intValue;
        _sourceJsonElement = sourceJsonElement;
    }

    FilterPropertyValue(double doubleValue, JsonElement sourceJsonElement)
    {
        DoubleValue = doubleValue;
        _sourceJsonElement = sourceJsonElement;
    }

    FilterPropertyValue(string stringValue, JsonElement sourceJsonElement)
    {
        StringValue = stringValue;
        _sourceJsonElement = sourceJsonElement;
    }

    FilterPropertyValue(IReadOnlyList<double> listOfDoubles, JsonElement sourceJsonElement)
    {
        ListOfDoubles = listOfDoubles;
        _sourceJsonElement = sourceJsonElement;
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
        return this switch
        {
            { ListOfInts: { } intList } when overrideValue is int overrideIntValue => intList.Contains(overrideIntValue),
            { ListOfInts: { } intList } when overrideValue is double overrideDoubleValue => intList.Select(i => (double)i).Contains(overrideDoubleValue),
            { ListOfDoubles: { } doubleList } when overrideValue is double overrideDoubleValue => doubleList.Contains(overrideDoubleValue),
            { ListOfDoubles: { } doubleList } when overrideValue is int overrideIntValue => doubleList.Contains(overrideIntValue),
            { ListOfStrings: { } stringList } when overrideValue is string overrideStringValue => stringList.Contains(overrideStringValue, StringComparer.OrdinalIgnoreCase),
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
    /// <param name="other">The <see cref="FilterPropertyValue"/> to compare with.</param>
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
            { StringValue: { } stringValue } => string.Compare(stringValue, other.ToString(), StringComparison.OrdinalIgnoreCase),
            { DoubleValue: { } doubleValue } when other is double overrideDouble => doubleValue.CompareTo(overrideDouble),
            { DoubleValue: { } doubleValue } when other is int overrideInt => doubleValue.CompareTo(overrideInt),
            { IntValue: { } intValue } => intValue.CompareTo(other),
            { BoolValue: { } boolValue } => boolValue.CompareTo(other),
            _ => 1
        };
    }


    public static bool operator >(FilterPropertyValue left, object? right) => NotNull(left).CompareTo(right) > 0;
    public static bool operator <(FilterPropertyValue? left, object? right) => NotNull(left).CompareTo(left) < 0;
    public static bool operator >=(FilterPropertyValue left, object? right) => NotNull(left).CompareTo(right) >= 0;
    public static bool operator <=(FilterPropertyValue left, object? right) => NotNull(left).CompareTo(right) <= 0;

    public override string ToString()
    {
        return this switch
        {
            { StringValue: { } stringValue } => stringValue,
            { ListOfInts: { } intArrayValue } => string.Join(", ", intArrayValue),
            { ListOfDoubles: { } doubleArrayValue } => string.Join(", ", doubleArrayValue),
            { DoubleValue: { } doubleValue } => doubleValue.ToString(CultureInfo.InvariantCulture),
            { IntValue: { } intValue } => intValue.ToString(CultureInfo.InvariantCulture),
            { BoolValue: { } boolValue } => boolValue.ToString(),
            _ => string.Empty
        };
    }

    /// <summary>
    /// Determines if this instance is equal to the specified <paramref name="other"/> <see cref="FilterPropertyValue"/>
    /// instance. This should not be used when evaluating filter property conditions.
    /// </summary>
    /// <param name="other">The <see cref="FilterPropertyValue"/> to compare to.</param>
    /// <returns><c>true</c> if these represent the same filter property value.</returns>
    public bool Equals(FilterPropertyValue? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return JsonComparison.AreJsonElementsEqual(_sourceJsonElement, other._sourceJsonElement);
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
        [NotNullWhen(returnValue: true)] out IReadOnlyList<int>? value)
    {
        List<int> values = new();
        foreach (var element in jsonElement.EnumerateArray())
        {
            if (element.ValueKind is not JsonValueKind.Number || !element.TryGetInt32(out var intValue))
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