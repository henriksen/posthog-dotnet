using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PostHog.Api;
using PostHog.Features;
using PostHog.Json;

namespace LocalEvaluatorTests;

public class TheEvaluateFeatureFlagMethod
{
    static LocalEvaluationApiResult CreateFlags(string key, IReadOnlyList<PropertyFilter> properties)
    {
        return new LocalEvaluationApiResult(
            Flags: [
                new LocalFeatureFlag(
                    Id: 42,
                    TeamId: 23,
                    Name: $"{key}-feature-flag",
                    Key: key,
                    Filters: new FeatureFlagFilters(
                        Groups: [
                            new FeatureFlagGroup(
                                Properties: properties
                            )
                        ]
                    ))
            ],
            GroupTypeMapping: new Dictionary<string, string>()
        );
    }

    [Theory]
    [InlineData("snuffleupagus@gmail.com", ComparisonOperator.Regex, "^.*?@gmail.com$", true)]
    [InlineData("snuffleupagus@hotmail.com", ComparisonOperator.Regex, "^.*?@gmail.com$", false)]
    [InlineData("snuffleupagus@gmail.com", ComparisonOperator.NotRegex, "^.*?@gmail.com$", false)]
    [InlineData("snuffleupagus@hotmail.com", ComparisonOperator.NotRegex, "^.*?@gmail.com$", true)]
    // PostHog supports this for number types.
    [InlineData(8675309, ComparisonOperator.Regex, ".+75.+", true)]
    [InlineData(8675309, ComparisonOperator.NotRegex, ".+75.+", false)]
    [InlineData(8675309, ComparisonOperator.Regex, ".+76.+", false)]
    [InlineData(8675309, ComparisonOperator.NotRegex, ".+76.+", true)]
    public void MatchesRegexUserProperty(object overrideValue, ComparisonOperator comparison, string filterValue, bool expected)
    {
        var flags = CreateFlags(
            key: "email",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "email",
                    Value: new PropertyFilterValue(filterValue),
                    Operator: comparison)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["email"] = overrideValue
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "email",
            distinctId: "distinct-id",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Works at PostHog", ComparisonOperator.ContainsIgnoreCase, "\"posthog\"", true)]
    [InlineData("Works at PostHog", ComparisonOperator.DoesNotContainsIgnoreCase, "\"posthog\"", false)]
    [InlineData("Works at PostHog", ComparisonOperator.DoesNotContainsIgnoreCase, "\"PostHog\"", false)]
    [InlineData("Loves puppies", ComparisonOperator.ContainsIgnoreCase, "\"cats\"", false)]
    [InlineData("Loves puppies", ComparisonOperator.DoesNotContainsIgnoreCase, "\"cats\"", true)]
    public void HandlesContainsComparisons(object overrideValue, ComparisonOperator comparison, string filterValueJson, bool expected)
    {
        var flags = CreateFlags(
            key: "bio",
            properties:
            [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "bio",
                    Value: PropertyFilterValue.Create(JsonDocument.Parse(filterValueJson).RootElement)!,
                    Operator: comparison)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["bio"] = overrideValue
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "bio",
            distinctId: "distinct-id",
            personProperties: properties);

        Assert.Equal(expected, result);
    }


    [Theory]
    [InlineData(22, ComparisonOperator.GreaterThan, "\"21\"", true)]
    [InlineData(22, ComparisonOperator.GreaterThanOrEquals, "\"21\"", true)]
    [InlineData("22", ComparisonOperator.GreaterThan, "\"21\"", true)]
    [InlineData("22", ComparisonOperator.GreaterThanOrEquals, "\"21\"", true)]
    [InlineData(20, ComparisonOperator.GreaterThan, "\"21\"", false)]
    [InlineData(20, ComparisonOperator.GreaterThanOrEquals, "\"21\"", false)]
    [InlineData("20", ComparisonOperator.GreaterThan, "\"21\"", false)]
    [InlineData("20", ComparisonOperator.GreaterThanOrEquals, "\"21\"", false)]
    [InlineData(22, ComparisonOperator.LessThan, "\"21\"", false)]
    [InlineData(22, ComparisonOperator.LessThanOrEquals, "\"21\"", false)]
    [InlineData("22", ComparisonOperator.LessThan, "\"21\"", false)]
    [InlineData("22", ComparisonOperator.LessThanOrEquals, "\"21\"", false)]
    [InlineData(20, ComparisonOperator.LessThan, "\"21\"", true)]
    [InlineData(20, ComparisonOperator.LessThanOrEquals, "\"21\"", true)]
    [InlineData("20", ComparisonOperator.LessThan, "\"21\"", true)]
    [InlineData("20", ComparisonOperator.LessThanOrEquals, "\"21\"", true)]
    [InlineData(21, ComparisonOperator.GreaterThanOrEquals, "\"21\"", true)]
    [InlineData("21", ComparisonOperator.GreaterThanOrEquals, "\"21\"", true)]
    [InlineData(21, ComparisonOperator.LessThanOrEquals, "\"21\"", true)]
    [InlineData("21", ComparisonOperator.LessThanOrEquals, "\"21\"", true)]
    public void HandlesGreaterAndLessThanComparisons(object overrideValue, ComparisonOperator comparison, string filterValueJson, bool expected)
    {
        var flags = CreateFlags(
            key: "age",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "age",
                    Value: PropertyFilterValue.Create(JsonDocument.Parse(filterValueJson).RootElement)!,
                    Operator: comparison)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["age"] = overrideValue
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "age",
            distinctId: "distinct-id",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("tyrion@example.com", true)]
    [InlineData("TYRION@example.com", true)] // Case-insensitive
    [InlineData("nobody@example.com", false)]
    public void HandlesExactMatchWithStringValuesArray(string email, bool expected)
    {
        var flags = CreateFlags(
            key: "email",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "email",
                    Value: new PropertyFilterValue([
                        "tyrion@example.com",
                        "danaerys@example.com",
                        "sansa@example.com",
                        "ned@example.com"
                    ]),
                    Operator: ComparisonOperator.Exact)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["email"] = email
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "email",
            distinctId: "1234",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(42, true)]
    [InlineData(42.5, true)]
    [InlineData("42.5", true)]
    [InlineData(21, false)]
    [InlineData("42", true)]
    [InlineData("21", false)]
    public void HandlesExactMatchNumericValues(object ageOverride, bool expected)
    {
        var flags = CreateFlags(
            key: "age",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "age",
                    Value: new PropertyFilterValue(["4", "8", "15", "16", "23", "42", "42.5" ]),
                    Operator: ComparisonOperator.Exact)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["age"] = ageOverride
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "age",
            distinctId: "1234",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("-30h", "2024-01-21T16:15:49Z", true)]
    [InlineData("-30h", "2024-01-21T16:15:51Z", false)]
    [InlineData("-24d", "2023-12-29T22:15:49Z", true)]
    [InlineData("-24d", "2023-12-29T22:15:51Z", false)]
    [InlineData("-2w", "2024-01-08T22:15:49Z", true)]
    [InlineData("-2w", "2024-01-08T22:15:51Z", false)]
    [InlineData("-1m", "2023-12-22T22:15:49Z", true)]
    [InlineData("-1m", "2023-12-22T22:15:51Z", false)]
    [InlineData("-1y", "2023-01-22T22:15:49Z", true)]
    [InlineData("-1y", "2023-01-22T22:15:51Z", false)]
    public void CanPerformIsDateBeforeComparisonCorrectly(string relativeDateString, string joinDate, bool expected)
    {
        var timeProvider = new FakeTimeProvider();
        var now = DateTimeOffset.Parse("2024-01-22T22:15:50Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(now);
        var flags = CreateFlags(
            key: "join_date",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "join_date",
                    Value: new PropertyFilterValue(relativeDateString),
                    Operator: ComparisonOperator.IsDateBefore)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["join_date"] = DateTimeOffset.Parse(joinDate, CultureInfo.InvariantCulture)
        };
        var localEvaluator = new LocalEvaluator(flags, timeProvider, NullLogger<LocalEvaluator>.Instance);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "join_date",
            distinctId: "1234b",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("-30h", "2024-01-21T16:15:49Z", false)]
    [InlineData("-30h", "2024-01-21T16:15:51Z", true)]
    [InlineData("-24d", "2023-12-29T22:15:49Z", false)]
    [InlineData("-24d", "2023-12-29T22:15:51Z", true)]
    [InlineData("-2w", "2024-01-08T22:15:49Z", false)]
    [InlineData("-2w", "2024-01-08T22:15:51Z", true)]
    [InlineData("-1m", "2023-12-22T22:15:49Z", false)]
    [InlineData("-1m", "2023-12-22T22:15:51Z", true)]
    [InlineData("-1y", "2023-01-22T22:15:49Z", false)]
    [InlineData("-1y", "2023-01-22T22:15:51Z", true)]
    public void CanPerformIsDateAfterComparisonCorrectly(string relativeDateString, string joinDate, bool expected)
    {
        var timeProvider = new FakeTimeProvider();
        var now = DateTimeOffset.Parse("2024-01-22T22:15:50Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(now);
        var flags = CreateFlags(
            key: "join_date",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "join_date",
                    Value: new PropertyFilterValue(relativeDateString),
                    Operator: ComparisonOperator.IsDateAfter)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["join_date"] = DateTimeOffset.Parse(joinDate, CultureInfo.InvariantCulture)
        };
        var localEvaluator = new LocalEvaluator(flags, timeProvider, NullLogger<LocalEvaluator>.Instance);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "join_date",
            distinctId: "1234b",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("-30h", "2024-01-21T16:15:49Z", true)]
    [InlineData("-30h", "2024-01-21T16:15:51Z", false)]
    [InlineData("-24d", "2023-12-29T22:15:49Z", true)]
    [InlineData("-24d", "2023-12-29T22:15:51Z", false)]
    [InlineData("-2w", "2024-01-08T22:15:49Z", true)]
    [InlineData("-2w", "2024-01-08T22:15:51Z", false)]
    [InlineData("-1m", "2023-12-22T22:15:49Z", true)]
    [InlineData("-1m", "2023-12-22T22:15:51Z", false)]
    [InlineData("-1y", "2023-01-22T22:15:49Z", true)]
    [InlineData("-1y", "2023-01-22T22:15:51Z", false)]
    public void CanPerformIsDateBeforeComparisonCorrectlyWhenPropertyIsString(string relativeDateString, string joinDate, bool expected)
    {
        var timeProvider = new FakeTimeProvider();
        var now = DateTimeOffset.Parse("2024-01-22T22:15:50Z", CultureInfo.InvariantCulture);
        timeProvider.SetUtcNow(now);
        var properties = new Dictionary<string, object?>
        {
            ["join_date"] = joinDate
        };
        var flags = CreateFlags(
            key: "join_date",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "join_date",
                    Value: new PropertyFilterValue(relativeDateString),
                    Operator: ComparisonOperator.IsDateBefore)
            ]
        );
        var localEvaluator = new LocalEvaluator(flags, timeProvider, NullLogger<LocalEvaluator>.Instance);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "join_date",
            distinctId: "some-distinct-id",
            personProperties: properties);

        Assert.Equal(expected, result);
    }


}