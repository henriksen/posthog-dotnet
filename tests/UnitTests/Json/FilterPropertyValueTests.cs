
using System.Text.Json;
using PostHog.Json;

namespace FilterPropertyValueTests;

public class TheIsExactMatchMethod
{
    [Fact]
    public void ReturnsTrueWhenPropertyValueMatchesString()
    {
        var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse("\"scooby\"").RootElement);
        object matchingOverrideValue = "scooby";
        object anotherMatchingOverrideValue = "SCoObY";
        object notMatchingOverrideValue = "shaggy";

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenPropertyValueIsInStringArray()
    {
        var json = """["scooby", "shaggy", "velma", "daphne"]""";
        var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = "scooby";
        object anotherMatchingOverrideValue = "SCoObY";
        object notMatchingOverrideValue = "fred";

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenPropertyValueMatchesInt()
    {
        var json = """42""";
        var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 42;
        object anotherMatchingOverrideValue = 42.0;
        object notMatchingOverrideValue = 21;
        object anotherNotMatchingOverrideValue = 21.3;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(anotherNotMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenPropertyValueMatchesDouble()
    {
        var json = """42.23""";
        var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 42.23;
        object anotherMatchingOverrideValue = 42.230;
        object notMatchingOverrideValue = 42;
        object anotherNotMatchingOverrideValue = 42.239;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(anotherNotMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenPropertyValueIsInIntArray()
    {
        var json = """[4, 8, 15, 16, 23, 42 ]""";
        var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 42;
        object anotherMatchingOverrideValue = 42.0;
        object notMatchingOverrideValue = 21;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenPropertyValueIsInDoubleArray()
    {
        var json = """[4.1, 8.2, 15.3, 16.4, 23.5, 42 ]""";
        var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 23.5;
        object anotherMatchingOverrideValue = 42;
        object notMatchingOverrideValue = 23.01;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
    }
}
