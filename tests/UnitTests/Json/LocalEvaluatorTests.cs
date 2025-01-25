
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using PostHog.Api;
using PostHog.Json;
using PostHog.Library;

namespace LocalEvaluatorTests;

public class TheMatchPropertyMethod
{
    [Fact]
    public void MatchesRegexUserProperty()
    {
        var properties = new Dictionary<string, object?>
        {
            ["email"] = "snuffleupagus@gmail.com"
        };
        var filterProperty = new FilterProperty(
            Key: "email",
            Type: "person",
            Value: JsonDocument.Parse(json: "\"^.*?@gmail.com$\"").RootElement,
            Operator: ComparisonType.Regex);
        var localEvaluator = new LocalEvaluator();

        var result = localEvaluator.MatchProperty(filterProperty, properties);

        Assert.True(result);
    }

    [Theory]
    [InlineData("tyrion@example.com", true)]
    [InlineData("TYRION@example.com", true)] // Case-insensitive
    [InlineData("nobody@example.com", false)]
    public void HandlesExactMatchWithStringValuesArray(string email, bool expected)
    {
        var valuesJson = """
                         [
                           "tyrion@example.com",
                           "danaerys@example.com",
                           "sansa@example.com",
                           "ned@example.com"
                         ]
                         """;
        var filterProperty = new FilterProperty(
            Key: "email",
            Type: "person",
            Value: JsonDocument.Parse(json: valuesJson).RootElement,
            Operator: ComparisonType.Exact);
        var properties = new Dictionary<string, object?>
        {
            ["email"] = email
        };
        var localEvaluator = new LocalEvaluator();

        var result = localEvaluator.MatchProperty(filterProperty, properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(42, true)]
    [InlineData(21, false)]
    public void HandlesExactMatchWithIntValuesArray(int age, bool expected)
    {
        var valuesJson = """
                         [
                           4, 8, 15, 16, 23, 42 
                         ]
                         """;
        var filterProperty = new FilterProperty(
            Key: "age",
            Type: "person",
            Value: JsonDocument.Parse(json: valuesJson).RootElement,
            Operator: ComparisonType.Exact);
        var properties = new Dictionary<string, object?>
        {
            ["age"] = age
        };
        var localEvaluator = new LocalEvaluator();

        var result = localEvaluator.MatchProperty(filterProperty, properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(42.42, true)]
    [InlineData(23.49, false)]
    public void HandlesExactMatchWithDoubleValuesArray(double age, bool expected)
    {
        var valuesJson = """
                         [
                           4.1, 8.2, 15.3, 16.4, 23.5, 42.42 
                         ]
                         """;
        var filterProperty = new FilterProperty(
            Key: "cash",
            Type: "person",
            Value: JsonDocument.Parse(json: valuesJson).RootElement,
            Operator: ComparisonType.Exact);
        var properties = new Dictionary<string, object?>
        {
            ["cash"] = age
        };
        var localEvaluator = new LocalEvaluator();

        var result = localEvaluator.MatchProperty(filterProperty, properties);

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
        var properties = new Dictionary<string, object?>
        {
            ["join_date"] = DateTimeOffset.Parse(joinDate, CultureInfo.InvariantCulture)
        };
        var filterProperty = new FilterProperty(
            Key: "join_date",
            Type: "person",
            Value: JsonDocument.Parse(json: $"\"{relativeDateString}\"").RootElement,
            Operator: ComparisonType.IsDateBefore);
        var localEvaluator = new LocalEvaluator(timeProvider);

        var result = localEvaluator.MatchProperty(filterProperty, properties);

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
        var properties = new Dictionary<string, object?>
        {
            ["join_date"] = DateTimeOffset.Parse(joinDate, CultureInfo.InvariantCulture)
        };
        var filterProperty = new FilterProperty(
            Key: "join_date",
            Type: "person",
            Value: JsonDocument.Parse(json: $"\"{relativeDateString}\"").RootElement,
            Operator: ComparisonType.IsDateAfter);
        var localEvaluator = new LocalEvaluator(timeProvider);

        var result = localEvaluator.MatchProperty(filterProperty, properties);

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
        var filterProperty = new FilterProperty(
            Key: "join_date",
            Type: "person",
            Value: JsonDocument.Parse(json: $"\"{relativeDateString}\"").RootElement,
            Operator: ComparisonType.IsDateBefore);
        var localEvaluator = new LocalEvaluator(timeProvider);

        var result = localEvaluator.MatchProperty(filterProperty, properties);

        Assert.Equal(expected, result);
    }
}