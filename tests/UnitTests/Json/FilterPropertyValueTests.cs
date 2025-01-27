
using System.Text.Json;
using PostHog.Json;

namespace PropertyFilterValueTests;

public class TheIsExactMatchMethod
{
    [Fact]
    public void ReturnsTrueWhenPropertyValueMatchesString()
    {
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse("\"scooby\"").RootElement);
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
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse(json).RootElement);
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
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 42;
        object anotherMatchingOverrideValue = 42.0;
        object anotherNotMatchingOverrideValueButClose = 42.3;
        object notMatchingOverrideValue = 21;
        object anotherNotMatchingOverrideValue = 21.3;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(anotherNotMatchingOverrideValueButClose));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(anotherNotMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenPropertyValueMatchesDouble()
    {
        var json = """42.23""";
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse(json).RootElement);
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
    public void ReturnsTrueWhenPropertyValueIsInInt64Array()
    {
        var json = """[4, 8, 15, 16, 23, 42, 21474836470]""";
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 42;
        object matchingLongValue = 21474836470;
        object anotherMatchingOverrideValue = 42.0;
        object notMatchingOverrideValue = 21;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(matchingLongValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenInt32PropertyValueIsInDoubleArray()
    {
        var json = """[4.1, 8.2, 15.3, 16.4, 23.5, 42 ]""";
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse(json).RootElement);
        object matchingOverrideValue = 23.5;
        object anotherMatchingOverrideValue = 42;
        object notMatchingOverrideValue = 23.01;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matchingOverrideValue));
        Assert.True(filterPropertyValue.IsExactMatch(anotherMatchingOverrideValue));
        Assert.False(filterPropertyValue.IsExactMatch(notMatchingOverrideValue));
    }

    [Fact]
    public void ReturnsTrueWhenInt64PropertyValueIsInDoubleArray()
    {
        var json = """[4.1, 8.2, 15.3, 16.4, 21474836470, 42 ]""";
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse(json).RootElement);
        object matching = 21474836470;

        Assert.NotNull(filterPropertyValue);
        Assert.True(filterPropertyValue.IsExactMatch(matching));
    }
}

public class TheEqualsMethod
{
    [Fact]
    public void CanCompareTwoIntegers()
    {
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse("21474836480").RootElement);
        var comparand = new PropertyFilterValue(21474836480);

        Assert.NotNull(filterPropertyValue);
        Assert.Equal(21474836480, filterPropertyValue.IntegerValue);
        Assert.Equal(comparand, filterPropertyValue);
    }

    [Fact]
    public void CanCompareTwoDoubles()
    {
        var filterPropertyValue = PropertyFilterValue.Create(JsonDocument.Parse("42.5").RootElement);
        var comparand = new PropertyFilterValue(42.5);

        Assert.NotNull(filterPropertyValue);
        Assert.Equal(42.5, filterPropertyValue.DoubleValue);
        Assert.Equal(comparand, filterPropertyValue);
    }
}
