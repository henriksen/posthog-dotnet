using PostHog.Api;
using PostHog.Library;
using static PostHog.Library.Ensure;

namespace PostHog.Json;

/// <summary>
/// Class used to locally evaluate feature flags.
/// </summary>
public record LocalEvaluator(TimeProvider TimeProvider)
{
    /// <summary>
    /// A default constructor that uses <see cref="System.TimeProvider.System"/>.
    /// </summary>
    public LocalEvaluator() : this(TimeProvider.System) { }

    /// <summary>
    /// Evaluates a feature flag for a given set of properties.
    /// </summary>
    /// <remarks>
    /// Only looks for matches where the key exists in properties.
    /// Doesn't support the operator <c>is_not_set</c>.
    /// </remarks>
    /// <param name="filterProperty">The <see cref="FilterProperty"/> to evaluate.</param>
    /// <param name="properties">The overriden values that describe the user/group.</param>
    /// <returns><c>true</c> if the current user/group matches the property. Otherwise <c>false</c>.</returns>
    public bool MatchProperty(FilterProperty filterProperty, Dictionary<string, object?> properties)
    {
        var key = NotNull(filterProperty).Key;
        var value = FilterPropertyValue.Create(filterProperty.Value)
            ?? throw new InconclusiveMatchException("The filter property value is null");

        // The overrideValue is the value that the user or group has set for the property. It's called "override value"
        // because when passing it to the `/decide` endpoint, it overrides the values stored in PostHog. For local
        // evaluation, it's a bit of a misnomer because this is the *only* value we're concerned with. I thought about
        // naming this to comparand but wanted to keep the naming consistent with the other client libraries.
        // @haacked
        if (!NotNull(properties).TryGetValue(key, out var overrideValue))
        {
            throw new InconclusiveMatchException("Can't match properties without a given property value");
        }

        if (overrideValue is null && filterProperty.Operator != ComparisonType.IsNot)
        {
            // If the value is null, just fail the feature flag comparison. This doesn't throw an
            // InconclusiveMatchException because the property value was provided.
            return false;
        }

        return filterProperty.Operator switch
        {
            ComparisonType.Exact => value.IsExactMatch(overrideValue),
            ComparisonType.IsNot => !value.IsExactMatch(overrideValue),
            ComparisonType.GreaterThan => value > overrideValue,
            ComparisonType.LessThan => value < overrideValue,
            ComparisonType.GreaterThanOrEquals => value >= overrideValue,
            ComparisonType.LessThanOrEquals => value <= overrideValue,
            ComparisonType.ContainsIgnoreCase => value.Contains(overrideValue, StringComparison.OrdinalIgnoreCase),
            ComparisonType.DoesNotContainsIgnoreCase => !value.Contains(overrideValue, StringComparison.OrdinalIgnoreCase),
            ComparisonType.Regex => value.IsRegexMatch(overrideValue),
            ComparisonType.NotRegex => !value.IsRegexMatch(overrideValue),
            ComparisonType.IsSet => true, // We already checked to see that the key exists.
            ComparisonType.IsDateBefore => value.IsDateBefore(overrideValue, TimeProvider.GetUtcNow()),
            ComparisonType.IsDateAfter => !value.IsDateBefore(overrideValue, TimeProvider.GetUtcNow()),
            _ => throw new InconclusiveMatchException($"Unknown operator: {filterProperty.Operator}")
        };
    }
}