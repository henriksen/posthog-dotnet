using System.Collections.ObjectModel;
using System.Text.Json;
using PostHog.Api;
using PostHog.Json;

namespace JsonSerializerHelperTests;

public class TheSerializeToCamelCaseJsonMethod
{
    [Fact]
    public async Task ShouldSerializeObjectToCamelCaseJson()
    {
        var obj = new { PropertyOne = "value", PropertyTwo = 1 };

        var json = await JsonSerializerHelper.SerializeToCamelCaseJsonStringAsync(obj);

        Assert.Equal("{\"propertyOne\":\"value\",\"propertyTwo\":1}", json);
    }

    [Fact]
    public async Task CanSerializeFilterProperty()
    {
        var obj = new FilterProperty(
            Key: "$group_key",
            Type: "group",
            Value: JsonDocument.Parse("\"01943db3-83be-0000-e7ea-ecae4d9b5afb\"").RootElement,
            Operator: ComparisonType.Exact,
            GroupTypeIndex: 2);

        var json = await JsonSerializerHelper.SerializeToCamelCaseJsonStringAsync(obj, writeIndented: true);

        Assert.Equal("""
                     {
                       "key": "$group_key",
                       "type": "group",
                       "value": "01943db3-83be-0000-e7ea-ecae4d9b5afb",
                       "operator": "exact",
                       "group_type_index": 2
                     }
                     """, json);
    }
}

public class TheDeserializeFromCamelCaseJsonMethod
{
    [Fact]
    public async Task CanDeserializeJsonToDecideApiResult()
    {
        var json = await File.ReadAllTextAsync("./Json/decide-api-result-v3.json");

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<DecideApiResult>(json);

        Assert.NotNull(result?.Config);
        Assert.NotNull(result.Analytics);
        Assert.True(result.Config.EnableCollectEverything);
        Assert.False(result.IsAuthenticated);
        Assert.Equal(new Dictionary<string, StringOrValue<bool>>()
        {
            ["hogtied_got_character"] = "danaerys",
            ["hogtied-homepage-user"] = true,
            ["hogtied-homepage-bonanza"] = true
        }, result.FeatureFlags);
        Assert.Equal("/i/v0/e/", result.Analytics.Endpoint);
        Assert.True(result.DefaultIdentifiedOnly);
        Assert.False(result.ErrorsWhileComputingFlags);
        Assert.Equal(new Dictionary<string, string>
        {
            ["hogtied_got_character"] = "{\"role\": \"khaleesi\"}",
            ["hogtied-homepage-user"] = "{\"is_cool\": true}"
        }, result.FeatureFlagPayloads);
    }

    [Fact]
    public async Task CanDeserializeNegatedJsonToDecideApiResult()
    {
        var json = await File.ReadAllTextAsync("./Json/decide-api-result-v3-negated.json");

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<DecideApiResult>(json);

        Assert.NotNull(result?.Config);
        Assert.NotNull(result.Analytics);
        Assert.NotNull(result.FeatureFlagPayloads);
        Assert.False(result.Config.EnableCollectEverything);
        Assert.True(result.IsAuthenticated);
        Assert.Equal(new Dictionary<string, StringOrValue<bool>>()
        {
            ["hogtied_got_character"] = false,
            ["hogtied-homepage-user"] = false,
            ["hogtied-homepage-bonanza"] = false
        }, result.FeatureFlags);
        Assert.Equal("/i/v0/e/", result.Analytics.Endpoint);
        Assert.False(result.DefaultIdentifiedOnly);
        Assert.True(result.ErrorsWhileComputingFlags);
        Assert.Empty(result.FeatureFlagPayloads);
    }

    [Fact]
    public async Task CanDeserializeLocalEvaluationApiResult()
    {
        var json = await File.ReadAllTextAsync("./Json/local-evaluation-api-result.json");

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<LocalEvaluationApiResult>(json);

        Assert.NotNull(result);
        Assert.Equal(3, result.Flags.Count);
        var firstFlag = result.Flags[0];
        Assert.Equal(91866, firstFlag.Id);
        Assert.Equal(110510, firstFlag.TeamId);
        Assert.Equal("A multivariate feature flag that tells you what character you are", firstFlag.Name);
        Assert.Equal("hogtied_got_character", firstFlag.Key);
        Assert.NotNull(firstFlag.Filters);
        var firstFlagGroup = Assert.Single(firstFlag.Filters.Groups);
        Assert.Null(firstFlagGroup.Variant);

        Assert.Equal(new FilterProperty(
                Key: "size",
                Type: "group",
                Value: JsonSerializer.SerializeToElement(new[] { "small" }),
                Operator: ComparisonType.Exact,
                GroupTypeIndex: 3),
            firstFlagGroup.Properties[0]
        );
        Assert.Equal([
                new FilterProperty(
                    Key: "size",
                    Type: "group",
                    Value: JsonSerializer.SerializeToElement(new[] { "small" }),
                    Operator: ComparisonType.Exact,
                    GroupTypeIndex: 3),
                new FilterProperty(
                    Key: "id",
                    Type: "cohort",
                    Value: JsonSerializer.SerializeToElement(1),
                    Operator: ComparisonType.In),
                new FilterProperty(
                    Key: "$group_key",
                    Type: "group",
                    Value: JsonSerializer.SerializeToElement("12345"),
                    Operator: ComparisonType.Exact,
                    GroupTypeIndex: 3)
            ],
            firstFlagGroup.Properties.ToList());
        Assert.Equal(100, firstFlagGroup.RolloutPercentage);
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["cersei"] = """{"role": "burn it all down"}""",
                ["tyrion"] = """{"role": "advisor"}""",
                ["danaerys"] = """{"role": "khaleesi"}""",
                ["jon-snow"] = """{"role": "king in the north"}"""
            },
            firstFlag.Filters.Payloads);
        Assert.NotNull(firstFlag.Filters.Multivariate);
        Assert.Equal([
            new Variant(Key: "tyrion", Name: "The one who talks", RolloutPercentage: 25),
            new Variant(Key: "danaerys", Name: "The mother of dragons", RolloutPercentage: 25),
            new Variant(Key: "jon-snow", Name: "Knows nothing", RolloutPercentage: 25),
            new Variant(Key: "cersei", Name: "Not nice", RolloutPercentage: 25),
        ], firstFlag.Filters.Multivariate.Variants);
        var secondFlag = result.Flags[1];
        Assert.Equal(91468, secondFlag.Id);
        Assert.Equal(110510, secondFlag.TeamId);
        Assert.Equal("Testing a PostHog client", secondFlag.Name);
        Assert.Equal("hogtied-homepage-user", secondFlag.Key);
        Assert.NotNull(secondFlag.Filters);
        var secondFlagGroup = Assert.Single(secondFlag.Filters.Groups);
        Assert.Null(secondFlagGroup.Variant);
        Assert.Equal(80, secondFlagGroup.RolloutPercentage);
        Assert.Equal([
            new FilterProperty(
                Key: "$group_key",
                Type: "group",
                Value: JsonSerializer.SerializeToElement("01943db3-83be-0000-e7ea-ecae4d9b5afb"),
                Operator: ComparisonType.Exact,
                GroupTypeIndex: 2),
        ], secondFlagGroup.Properties);
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["true"] = """{"is_cool": true}"""
            },
            secondFlag.Filters.Payloads);
        Assert.Null(secondFlag.Filters.Multivariate);
        Assert.False(secondFlag.Deleted);
        Assert.True(secondFlag.EnsureExperienceContinuity);

        var thirdFlag = result.Flags[2];
        Assert.Equal(1, thirdFlag.Id);
        Assert.Equal(42, thirdFlag.TeamId);
        Assert.Equal("File previews", thirdFlag.Name);
        Assert.Equal("file-previews", thirdFlag.Key);
        Assert.NotNull(thirdFlag.Filters);
        Assert.Equal([
            new FilterProperty(
                Key: "email",
                Type: "person",
                Value: JsonSerializer.SerializeToElement<string[]>(
                [
                    "tyrion@example.com",
                    "danaerys@example.com",
                    "sansa@example.com",
                    "ned@example.com"
                ]),
                Operator: ComparisonType.Exact)
        ], Assert.Single(thirdFlag.Filters.Groups).Properties);
        Assert.Equal(new Dictionary<string, string>
        {
            ["0"] = "account",
            ["1"] = "instance",
            ["2"] = "organization",
            ["3"] = "project",
            ["4"] = "company"
        }, result.GroupTypeMapping);
        Assert.Equal(new ReadOnlyDictionary<string, ConditionContainer>(new Dictionary<string, ConditionContainer>
        {
            ["1"] = new(
                    Type: "OR",
                    Values:
                    [
                        new ConditionGroup(
                            Type: "AND",
                            Values:
                            [
                                new FilterProperty(
                                    Key: "email",
                                    Operator: ComparisonType.IsSet,
                                    Type: "person",
                                    Value: JsonSerializer.SerializeToElement("is_set"))
                            ])
                    ])
        }),
            result.Cohorts);
    }

    [Fact]
    public async Task ShouldDeserializeApiResult()
    {
        var json = "{\"status\": 1}";

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<ApiResult>(json);

        Assert.NotNull(result);
        Assert.Equal(1, result.Status);
    }

    [Fact]
    public async Task CanDeserializeFilterProperty()
    {
        var json = """
                   {
                     "key": "$group_key",
                     "type": "group",
                     "value": "01943db3-83be-0000-e7ea-ecae4d9b5afb",
                     "operator": "exact",
                     "group_type_index": 2
                   }
                   """;

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<FilterProperty>(json);

        Assert.Equal(new FilterProperty(
            Key: "$group_key",
            Type: "group",
            Value: JsonDocument.Parse("\"01943db3-83be-0000-e7ea-ecae4d9b5afb\"").RootElement,
            Operator: ComparisonType.Exact,
            GroupTypeIndex: 2), result);
    }

    [Fact]
    public async Task CanDeserializePropertiesDictionaryWithNullValue()
    {
        var json = """
                   {
                     "size": "large",
                     "email": null
                   }
                   """;

        var result =
            await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<Dictionary<string, object?>>(json);

        Assert.NotNull(result);
        Assert.Equal("large", result["size"]?.ToString());
        Assert.Null(result["email"]);
    }

    [Fact]
    public async Task CanDeserializeStringOrBool()
    {
        var json = """
                   {
                     "TrueOrValue" : "danaerys",
                     "AnotherTrueOrValue" : true
                   }
                   """;

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<ClassWithStringOr>(json);

        Assert.NotNull(result);
        Assert.Equal("danaerys", result.TrueOrValue.StringValue);
        Assert.True(result.AnotherTrueOrValue.Value);
    }

    [Fact]
    public async Task CanDeserializeCamelCasedStringOrBool()
    {
        var json = """
                   {
                     "trueOrValue" : "danaerys",
                     "anotherTrueOrValue" : true
                   }
                   """;

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<ClassWithStringOr>(json);

        Assert.NotNull(result);
        Assert.Equal("danaerys", result.TrueOrValue.StringValue);
        Assert.True(result.AnotherTrueOrValue.Value);
    }

    [Fact]
    public async Task CanDeserializeStringOrBoolWithFalse()
    {
        var json = """
                   {
                     "TrueOrValue": "danaerys",
                     "AnotherTrueOrValue": false
                   }
                   """;

        var result = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<ClassWithStringOr>(json);

        Assert.NotNull(result);
        Assert.Equal("danaerys", result.TrueOrValue.StringValue);
        Assert.False(result.TrueOrValue.Value);
    }

    public class ClassWithStringOr
    {
        public StringOrValue<bool> TrueOrValue { get; set; } = null!;
        public StringOrValue<bool> AnotherTrueOrValue { get; set; } = null!;
    }
}