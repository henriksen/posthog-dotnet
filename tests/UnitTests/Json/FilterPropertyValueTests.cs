
using System.Text.Json;
using PostHog.Json;

public class FilterPropertyValueTests
{
    public class TheIsExactMatchMethod
    {
        [Fact]
        public void ReturnsTrueWhenPropertyValueMatchesString()
        {
            var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse("\"scooby\"").RootElement);
            object overrideValue = "scooby";
            object anotherOverrideValue = "shaggy";

            Assert.NotNull(filterPropertyValue);
            Assert.True(filterPropertyValue.IsExactMatch(overrideValue));
            Assert.False(filterPropertyValue.IsExactMatch(anotherOverrideValue));
        }

        [Fact]
        public void ReturnsTrueWhenPropertyValueIsInStringArray()
        {
            var json = """["scooby", "shaggy", "velma", "daphne"]""";
            var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
            object overrideValue = "scooby";
            object anotherOverrideValue = "fred";

            Assert.NotNull(filterPropertyValue);
            Assert.True(filterPropertyValue.IsExactMatch(overrideValue));
            Assert.False(filterPropertyValue.IsExactMatch(anotherOverrideValue));
        }

        [Fact]
        public void ReturnsTrueWhenPropertyValueIsInIntArray()
        {
            var json = """[4, 8, 15, 16, 23, 42 ]""";
            var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
            object overrideValue = 42;
            object anotherOverrideValue = 21;
            object yetAnotherOverrideValue = 42.0;

            Assert.NotNull(filterPropertyValue);
            Assert.True(filterPropertyValue.IsExactMatch(overrideValue));
            Assert.False(filterPropertyValue.IsExactMatch(anotherOverrideValue));
            Assert.True(filterPropertyValue.IsExactMatch(yetAnotherOverrideValue));
        }

        [Fact]
        public void ReturnsTrueWhenPropertyValueIsInDoubleArray()
        {
            var json = """[4.1, 8.2, 15.3, 16.4, 23.5, 42.42 ]""";
            var filterPropertyValue = FilterPropertyValue.Create(JsonDocument.Parse(json).RootElement);
            object overrideValue = 42.42;
            object anotherOverrideValue = 23.01;

            Assert.NotNull(filterPropertyValue);
            Assert.True(filterPropertyValue.IsExactMatch(overrideValue));
            Assert.False(filterPropertyValue.IsExactMatch(anotherOverrideValue));
        }
    }
}