
using System.Text.Json;
using PostHog.Api;
using PostHog.Json;

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

        var result = LocalEvaluator.MatchProperty(filterProperty, properties);

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

        var result = LocalEvaluator.MatchProperty(filterProperty, properties);

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

        var result = LocalEvaluator.MatchProperty(filterProperty, properties);

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

        var result = LocalEvaluator.MatchProperty(filterProperty, properties);

        Assert.Equal(expected, result);
    }
}
