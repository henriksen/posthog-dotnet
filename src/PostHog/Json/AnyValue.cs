using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using PostHog.Library;
using static PostHog.Library.Ensure;

namespace PostHog.Json;

/// <summary>
/// Similar to <see cref="StringOrValue{T}"/>, but can be any primitive type including arrays of primitives.
/// </summary>
/// <remarks>
/// This is intended to map to <c>any</c> in Python, but limited to the types returned by the PostHog API for
/// filter values.
/// </remarks>
public class AnyValue(string stringValue) : IComparable<AnyValue>, IEquatable<AnyValue>
{
    public string? StringValue { get; } = stringValue;

    [MemberNotNullWhen(true, nameof(StringValue))]
    public bool IsString { get; } = true;

    // Implicit greater than operator
    public static bool operator >(AnyValue left, AnyValue right) => NotNull(left).CompareTo(right) > 0;
    public static bool operator <(AnyValue left, AnyValue right) => NotNull(left).CompareTo(left) < 0;
    public static bool operator >=(AnyValue left, AnyValue right) => NotNull(left).CompareTo(right) >= 0;
    public static bool operator <=(AnyValue left, AnyValue right) => NotNull(left).CompareTo(right) <= 0;
    public static bool operator ==(AnyValue left, AnyValue right) => NotNull(left).CompareTo(right) == 0;
    public static bool operator !=(AnyValue left, AnyValue right) => !(left == right);

    /// <summary>
    /// Does a regular expression match on this instance with the specified <paramref name="other"/> instance.
    /// </summary>
    /// <param name="other"></param>
    /// <returns><c>true</c>If the current value is a valid regex and it matches the other value.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool IsRegexMatch(AnyValue other)
    {
        if (StringValue is null || !RegexHelpers.TryValidateRegex(StringValue, out var regex, RegexOptions.None))
        {
            return false;
        }

        if (NotNull(other).StringValue is { } value)
        {
            return regex.IsMatch(value);
        }

        throw new InvalidOperationException();
    }

    /// <summary>
    /// Returns a value indicating whether this instance contains the specified <paramref name="other"/> instance.
    /// </summary>
    /// <param name="other">The other value to compare to this one.</param>
    /// <param name="stringComparison">The type of comparison if these are strings.</param>
    /// <returns><c>true</c> if this instance contains the other.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool Contains(AnyValue? other, StringComparison stringComparison)
    {
        if (other is null)
        {
            return false;
        }

        if (IsString && other.IsString)
        {
            return StringValue.Contains(other.StringValue, stringComparison);
        }

        throw new NotImplementedException();
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
    /// <param name="other">The <see cref="AnyValue"/> to compare with.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public int CompareTo(AnyValue? other)
    {
        if (ReferenceEquals(other, null))
        {
            return 1;
        }

        if (IsString && other.IsString)
        {
            return string.Compare(StringValue, other.StringValue, StringComparison.OrdinalIgnoreCase);
        }

        throw new NotImplementedException();
    }

    public override bool Equals(object? obj) =>
        obj is AnyValue value && Equals(value)
        || obj is string
        && IsString
        && StringComparer.OrdinalIgnoreCase.Equals(StringValue, obj);

    public bool Equals(AnyValue? other)
        => other is not null && StringValue == other.StringValue && IsString == other.IsString;

    public override int GetHashCode() => HashCode.Combine(StringValue, IsString);

    public static bool Equals(AnyValue? left, AnyValue? right) => left?.Equals(right) ?? right is null;
}