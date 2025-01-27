using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PostHog;
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

    [Fact]
    public void MatchesRegexUserProperty()
    {
        var flags = CreateFlags(
            key: "email",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "email",
                    Value: new PropertyFilterValue("^.*?@gmail.com$"),
                    Operator: ComparisonOperator.Regex)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["email"] = "snuffleupagus@gmail.com"
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "email",
            distinctId: "distinct-id",
            personProperties: properties);

        Assert.Equal(true, result);
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
    [InlineData(21, false)]
    public void HandlesExactMatchWithIntValuesArray(int age, bool expected)
    {
        var flags = CreateFlags(
            key: "age",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "age",
                    Value: new PropertyFilterValue([4, 8, 15, 16, 23, 42 ]),
                    Operator: ComparisonOperator.Exact)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["age"] = age
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "age",
            distinctId: "1234",
            personProperties: properties);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(42.42, true)]
    [InlineData(23.49, false)]
    public void HandlesExactMatchWithDoubleValuesArray(double age, bool expected)
    {
        var flags = CreateFlags(
            key: "cash",
            properties: [
                new PropertyFilter(
                    Type: FilterType.Person,
                    Key: "cash",
                    Value: new PropertyFilterValue([4.1, 8.2, 15.3, 16.4, 23.5, 42.42]),
                    Operator: ComparisonOperator.Exact)
            ]
        );
        var properties = new Dictionary<string, object?>
        {
            ["cash"] = age
        };
        var localEvaluator = new LocalEvaluator(flags);

        var result = localEvaluator.EvaluateFeatureFlag(
            key: "cash",
            distinctId: "12341234",
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

    [Fact] // Ported from PostHog/posthog-python test_feature_flags.py
    public async Task CanMatchPersonProperties()
    {
        var json = """
                   {
                      "flags" : [
                         {
                            "id":1,
                            "name":"Beta Feature",
                            "key":"person-flag",
                            "is_simple_flag":true,
                            "active":true,
                            "filters":{
                               "groups":[
                                  {
                                     "properties":[
                                        {
                                           "key":"region",
                                           "operator":"exact",
                                           "value":[
                                              "USA"
                                           ],
                                           "type":"person"
                                        }
                                     ],
                                     "rollout_percentage":100
                                  }
                               ]
                            }
                         }
                      ]
                   }
                   """;
        var apiResult = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<LocalEvaluationApiResult>(json);
        Assert.NotNull(apiResult);
        var localEvaluator = new LocalEvaluator(apiResult);

        var match = localEvaluator.EvaluateFeatureFlag(
            key: "person-flag",
            distinctId: "some-distinct-id",
            personProperties: new Dictionary<string, object?> { ["region"] = "USA" });
        var notMatch = localEvaluator.EvaluateFeatureFlag(
            key: "person-flag",
            distinctId: "some-distinct-id-2",
            personProperties: new Dictionary<string, object?> { ["region"] = "Canada" });

        Assert.Equal(true, match);
        Assert.Equal(false, notMatch);
    }

    [Fact] // Ported from PostHog/posthog-python test_feature_flags.py
    public async Task CanMatchGroupProperties()
    {
        var json = """
                   {
                      "flags":[
                         {
                            "id":1,
                            "name":"Beta Feature",
                            "key":"group-flag",
                            "is_simple_flag":true,
                            "active":true,
                            "filters":{
                               "aggregation_group_type_index":0,
                               "groups":[
                                  {
                                     "properties":[
                                        {
                                           "group_type_index":0,
                                           "key":"name",
                                           "operator":"exact",
                                           "value":[
                                              "Project Name 1"
                                           ],
                                           "type":"group"
                                        }
                                     ],
                                     "rollout_percentage":35
                                  }
                               ]
                            }
                         }
                      ],
                      "group_type_mapping": {"0": "company", "1": "project"}
                   }
                   """;
        var apiResult = await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<LocalEvaluationApiResult>(json);
        Assert.NotNull(apiResult);
        var localEvaluator = new LocalEvaluator(apiResult);

        var noMatchBecauseNoGroupNames = localEvaluator.EvaluateFeatureFlag(
            key: "group-flag",
            distinctId: "some-distinct-id",
            groups:
            [
                new Group(
                    GroupType: "company",
                    GroupKey: "company",
                    Properties: new Dictionary<string, object?>
                    {
                        ["name"] = "Project Name 1"
                    })
            ]);
        var notMatch = localEvaluator.EvaluateFeatureFlag(
            key: "group-flag",
            distinctId: "some-distinct-2",
            groups:
            [
                new Group(
                    GroupType: "company",
                    GroupKey: "company",
                    Properties: new Dictionary<string, object?>
                    {
                        ["name"] = "Project Name 2"
                    })
            ]);
        var match = localEvaluator.EvaluateFeatureFlag(
            key: "group-flag",
            distinctId: "some-distinct-id",
            groups:
            [
                new Group(
                    GroupType: "company",
                    GroupKey: "amazon_without_rollout",
                    Properties: new Dictionary<string, object?>
                    {
                        ["name"] = "Project Name 1"
                    })
            ]);
        var notMatchBecauseRollout = localEvaluator.EvaluateFeatureFlag(
            key: "group-flag",
            distinctId: "some-distinct-id",
            groups:
            [
                new Group(
                    GroupType: "company",
                    GroupKey: "amazon",
                    Properties: new Dictionary<string, object?>
                    {
                        ["name"] = "Project Name 1"
                    })
            ]);
        var propertyMismatch = localEvaluator.EvaluateFeatureFlag(
            key: "group-flag",
            distinctId: "some-distinct-2",
            groups:
            [
                new Group(
                    GroupType: "company",
                    GroupKey: "amazon_without_rollout",
                    Properties: new Dictionary<string, object?>
                    {
                        ["name"] = "Project Name 2"
                    })
            ]);

        Assert.Equal(false, noMatchBecauseNoGroupNames);
        Assert.Equal(false, notMatch);
        Assert.Equal(true, match);
        Assert.Equal(false, propertyMismatch);
        Assert.Equal(false, notMatchBecauseRollout);
    }
}