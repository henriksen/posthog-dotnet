using PostHog.Api;
using PostHog.Json;
using static PostHog.Library.Ensure;

namespace PostHog.Library;

public record ComparisonOperator(ComparisonType ComparisonType)
{
    public bool IsMatch(AnyValue value, AnyValue overrideValue)
    {
        value = NotNull(value);

        return ComparisonType switch
        {
            ComparisonType.Exact => value == overrideValue,
            ComparisonType.IsNot => value != overrideValue,
            ComparisonType.GreaterThan => value > overrideValue,
            ComparisonType.LessThan => value < overrideValue,
            ComparisonType.GreaterThanOrEquals => value >= overrideValue,
            ComparisonType.LessThanOrEquals => value <= overrideValue,
            ComparisonType.ContainsIgnoreCase => value.Contains(overrideValue, StringComparison.OrdinalIgnoreCase),
            ComparisonType.DoesNotContainsIgnoreCase => !value.Contains(overrideValue, StringComparison.OrdinalIgnoreCase),
            ComparisonType.Regex => value.IsRegexMatch(overrideValue),
            ComparisonType.NotRegex => !value.IsRegexMatch(overrideValue),
            ComparisonType.IsSet => throw new NotImplementedException(),
            ComparisonType.IsDateBefore => throw new NotImplementedException(),
            ComparisonType.IsDateAfter => throw new NotImplementedException(),
            _ => throw new ArgumentException($"Unknown operator: {ComparisonType}")
        };
    }
}

