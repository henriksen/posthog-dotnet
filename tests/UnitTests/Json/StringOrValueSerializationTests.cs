using System.Text.Json;
using PostHog.Json;

namespace UnitTests.Json;

public class StringOrValueSerializationTests
{
    public class TestSubject
    {
        public StringOrValue<int> IntValue { get; set; } = null!;

        public StringOrValue<bool> BoolValue { get; set; } = null!;

        public NestedSubject Nested { get; set; } = null!;
    }

    public class NestedSubject
    {
        public StringOrValue<bool> BoolValue { get; set; } = null!;
    }

    public class TheDeserializeMethod
    {
        [Fact]
        public void CanDeserializeStringOrValueWithValues()
        {
            var json = """
                       {
                         "IntValue": 42,
                         "BoolValue": true,
                         "Nested": {"BoolValue": false}
                       }
                       """;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<TestSubject>(json, options);

            Assert.NotNull(result);
            Assert.Equal(42, result.IntValue.Value);
            Assert.True(result.BoolValue.Value);
            Assert.False(result.Nested.BoolValue.Value);
        }

        [Fact]
        public void CanDeserializeStringOrValueWithStringValues()
        {
            var json = """
                       {
                         "IntValue": "unavailable",
                         "BoolValue": "unavailable",
                         "Nested": {"BoolValue": "unavailable"}
                       }
                       """;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<TestSubject>(json, options);

            Assert.NotNull(result);
            Assert.Equal("unavailable", result.IntValue.StringValue);
            Assert.Equal("unavailable", result.BoolValue.StringValue);
            Assert.Equal("unavailable", result.Nested.BoolValue.StringValue);
        }
    }

    public class TheSerializeMethod
    {
        [Fact]
        public void CanSerializeSingleIntValue()
        {
            StringOrValue<int> value = 42;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Serialize(value, options);

            Assert.Equal("42", result);
        }

        [Fact]
        public void CanSerializeSingleString()
        {
            StringOrValue<int> value = "testing";
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Serialize(value, options);

            Assert.Equal("\"testing\"", result);
        }

        [Fact]
        public void CanSerializeObjectWithStringOrValue()
        {
            var source = new NestedSubject { BoolValue = true };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(source, options);

            Assert.Equal("{\"BoolValue\":true}", json);
        }

        [Fact]
        public void CanSerializeObjectWithStringOrStringValue()
        {
            var source = new NestedSubject { BoolValue = "unavailable" };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(source, options);

            Assert.Equal("{\"BoolValue\":\"unavailable\"}", json);
        }

        [Fact]
        public void CanSerializeObjectWithStringOrObjectValue()
        {
            var source = new { ObjectValue = new StringOrValue<object>(new { Nested = true }) };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(source, options);

            Assert.Equal("{\"ObjectValue\":{\"Nested\":true}}", json);
        }

        [Fact]
        public void CanSerializeObjectWithStringOrObjectStringValue()
        {
            var source = new { ObjectValue = new StringOrValue<object>("unavailable") };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(source, options);

            Assert.Equal("{\"ObjectValue\":\"unavailable\"}", json);
        }
    }

    public class TheImplicitOperators
    {
        [Fact]
        public void CanImplicitlyConvertIntToStringOrValue()
        {
            StringOrValue<int> value = 42;

            Assert.Equal(42, value.Value);
            Assert.Equal(42, value);
        }

        [Fact]
        public void CanImplicitlyConvertStringToStringOrValue()
        {
            StringOrValue<int> value = "hello";

            Assert.Equal("hello", value.StringValue);
            Assert.Equal("hello", value);
        }
    }
}