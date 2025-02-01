using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PostHog;
using PostHog.Api;
using PostHog.Cache;
using PostHog.Config;
using PostHog.Features;
using PostHog.Json;
using UnitTests.Fakes;

#pragma warning disable CA2000
namespace PostHogClientTests;

public class TheIsFeatureFlagEnabledAsyncMethod
{
    [Fact]
    public async Task CapturesFeatureFlagCalledEventOnlyOncePerDistinctIdFlagKeyAndResponse()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddLocalEvaluationResponse(
            """
            { 
              "flags": [
                {
                    "key": "flag-key",
                    "active": true,
                    "rollout_percentage": 100,
                    "filters": {
                        "groups": [
                            {
                                "properties": [],
                                "rollout_percentage": 100
                            }
                        ]
                    }
                }
              ]
            } 
            """
        );
        var captureRequestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        Assert.True(await client.IsFeatureEnabledAsync("flag-key", "a-distinct-id"));
        await client.IsFeatureEnabledAsync("flag-key", "a-distinct-id"); // Cache hit, not captured.
        Assert.True(await client.IsFeatureEnabledAsync("flag-key", "another-distinct-id"));
        await client.IsFeatureEnabledAsync("flag-key", "another-distinct-id"); // Cache hit

        client.ClearLocalFlagsCache();
        messageHandler.AddLocalEvaluationResponse(
            """
            { 
              "flags": [
                {
                    "key": "flag-key",
                    "active": true,
                    "rollout_percentage": 0,
                    "filters": {
                        "groups": [
                            {
                                "properties": [],
                                "rollout_percentage": 0
                            }
                        ]
                    }
                }
              ]
            } 
            """
        );
        Assert.False(await client.IsFeatureEnabledAsync("flag-key", "another-distinct-id")); // Not a cache-hit, new response

        await client.FlushAsync();
        var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal(
            $$"""
            {
              "api_key": "fake-project-api-key",
              "historical_migrations": false,
              "batch": [
                {
                  "event": "$feature_flag_called",
                  "properties": {
                    "$feature_flag": "flag-key",
                    "$feature_flag_response": true,
                    "locally_evaluated": false,
                    "$feature/flag-key": true,
                    "distinct_id": "a-distinct-id",
                    "$lib": "posthog-dotnet",
                    "$lib_version": "{{client.Version}}",
                    "$geoip_disable": true
                  },
                  "timestamp": "2024-01-21T19:08:23\u002B00:00"
                },
                {
                  "event": "$feature_flag_called",
                  "properties": {
                    "$feature_flag": "flag-key",
                    "$feature_flag_response": true,
                    "locally_evaluated": false,
                    "$feature/flag-key": true,
                    "distinct_id": "another-distinct-id",
                    "$lib": "posthog-dotnet",
                    "$lib_version": "{{client.Version}}",
                    "$geoip_disable": true
                  },
                  "timestamp": "2024-01-21T19:08:23\u002B00:00"
                },
                {
                  "event": "$feature_flag_called",
                  "properties": {
                    "$feature_flag": "flag-key",
                    "$feature_flag_response": false,
                    "locally_evaluated": false,
                    "$feature/flag-key": false,
                    "distinct_id": "another-distinct-id",
                    "$lib": "posthog-dotnet",
                    "$lib_version": "{{client.Version}}",
                    "$geoip_disable": true
                  },
                  "timestamp": "2024-01-21T19:08:23\u002B00:00"
                }
              ]
            }
            """
        , received);
    }

    [Fact]
    public async Task DoesNotCaptureFeatureFlagCalledEventWhenSendFeatureFlagsFalse()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedDecideResponse(
            count: 4,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"feature-value-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        await client.IsFeatureEnabledAsync(featureKey: "flag-key",
            distinctId: "a-distinct-id", options: new FeatureFlagOptions { SendFeatureFlagEvents = false });

        await client.FlushAsync();
        Assert.Empty(captureRequestHandler.ReceivedRequests);
    }
}

public class TheGetFeatureFlagAsyncMethod
{
    [Fact] // Ported from PostHog/posthog-python test_flag_person_properties
    public async Task MatchesOnPersonProperties()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
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
            """
        );
        var client = container.Activate<PostHogClient>();

        Assert.True(
            await client.GetFeatureFlagAsync(
                "person-flag",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new() { ["region"] = "USA" }
                })
        );
        Assert.False(
            await client.GetFeatureFlagAsync(
                "person-flag",
                distinctId: "some-distinct-2",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new() { ["region"] = "Canada" }
                })
        );
    }

    [Fact] // Ported from PostHog/posthog-python test_flag_group_properties
    public async Task MatchesOnGroupProperties()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
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
            """
        );
        var client = container.Activate<PostHogClient>();

        var noMatchBecauseNoGroupNames = await client.GetFeatureFlagAsync(
            featureKey: "group-flag",
            distinctId: "some-distinct-id",
            options: new FeatureFlagOptions
            {
                GroupProperties =
                [
                    new Group(
                        GroupType: "company",
                        GroupKey: "company",
                        Properties: new Dictionary<string, object?>
                        {
                            ["name"] = "Project Name 1"
                        })
                ]
            }
        );
        var noMatchBecauseCompanyNameDoesNotMatchFilter = await client.GetFeatureFlagAsync(
            featureKey: "group-flag",
            distinctId: "some-distinct-2",
            options: new FeatureFlagOptions
            {
                GroupProperties =
                [
                    new Group(
                        GroupType: "company",
                        GroupKey: "company",
                        Properties: new Dictionary<string, object?>
                        {
                            ["name"] = "Project Name 2"
                        })
                ]
            });
        var match = await client.GetFeatureFlagAsync(
            featureKey: "group-flag",
            distinctId: "some-distinct-id",
            options: new FeatureFlagOptions
            {
                GroupProperties =
                [
                    new Group(
                        GroupType: "company",
                        GroupKey: "amazon_without_rollout",
                        Properties: new Dictionary<string, object?>
                        {
                            ["name"] = "Project Name 1"
                        })
                ]
            });
        var notMatchBecauseRollout = await client.GetFeatureFlagAsync(
            featureKey: "group-flag",
            distinctId: "some-distinct-id",
            options: new FeatureFlagOptions
            {
                GroupProperties =
                [
                    new Group(
                        GroupType: "company",
                        GroupKey: "amazon",
                        Properties: new Dictionary<string, object?>
                        {
                            ["name"] = "Project Name 1"
                        })
                ]
            });
        var propertyMismatch = await client.GetFeatureFlagAsync(
            featureKey: "group-flag",
            distinctId: "some-distinct-2",
            options: new FeatureFlagOptions
            {
                GroupProperties =
                [
                    new Group(
                        GroupType: "company",
                        GroupKey: "amazon_without_rollout",
                        Properties: new Dictionary<string, object?>
                        {
                            ["name"] = "Project Name 2"
                        })
                ]
            }
        );

        // Going to fallback to decide
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {"group-flag": "decide-fallback-value"}}
            """
        );
        client.ClearLocalFlagsCache();
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
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
              "group_type_mapping": {}
            }
            """
        );
        var decideResult = await client.GetFeatureFlagAsync(
            featureKey: "group-flag",
            distinctId: "some-distinct-id",
            options: new FeatureFlagOptions
            {
                GroupProperties =
                [
                    new Group(
                        GroupType: "company",
                        GroupKey: "amazon",
                        Properties: new Dictionary<string, object?>
                        {
                            ["name"] = "Project Name 1"
                        })
                ]
            }
        );
        Assert.False(noMatchBecauseNoGroupNames);
        Assert.False(noMatchBecauseCompanyNameDoesNotMatchFilter);
        Assert.True(match);
        Assert.False(propertyMismatch);
        Assert.False(notMatchBecauseRollout);
        Assert.Equal("decide-fallback-value", decideResult);
    }

    [Fact] // Ported from PostHog/posthog-python test_flag_with_complex_definition
    public async Task ReturnsCorrectValueForComplexFlags()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
             "flags": [
                   {
                       "id": 1,
                       "name": "Beta Feature",
                       "key": "complex-flag",
                       "is_simple_flag": false,
                       "active": true,
                       "filters": {
                           "groups": [
                               {
                                   "properties": [
                                       {
                                           "key": "region",
                                           "operator": "exact",
                                           "value": ["USA"],
                                           "type": "person"
                                       },
                                       {
                                           "key": "name",
                                           "operator": "exact",
                                           "value": ["Aloha"],
                                           "type": "person"
                                       }
                                   ],
                                   "rollout_percentage": 100
                               },
                               {
                                   "properties": [
                                       {
                                           "key": "email",
                                           "operator": "exact",
                                           "value": ["a@b.com", "b@c.com"],
                                           "type": "person"
                                       }
                                   ],
                                   "rollout_percentage": 30
                               },
                               {
                                   "properties": [
                                       {
                                           "key": "doesnt_matter",
                                           "operator": "exact",
                                           "value": ["1", "2"],
                                           "type": "person"
                                       }
                                   ],
                                   "rollout_percentage": 0
                               }
                           ]
                       }
                   }
               ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        Assert.True(
            await client.GetFeatureFlagAsync(
                featureKey: "complex-flag",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new() { ["region"] = "USA", ["name"] = "Aloha" }
                })
        );

        // this distinctIDs hash is < rollout %
        Assert.True(
            await client.GetFeatureFlagAsync(
                featureKey: "complex-flag",
                distinctId: "some-distinct-id_within_rollout?",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new() { ["region"] = "USA", ["email"] = "a@b.com" }
                })
        );

        // will fall back on `/decide`, as all properties present for second group, but that group resolves to false
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {"complex-flag": "decide-fallback-value"}}
            """
        );
        Assert.Equal(
            "decide-fallback-value",
            await client.GetFeatureFlagAsync(
                featureKey: "complex-flag",
                distinctId: "some-distinct-id_outside_rollout?",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new()
                    {
                        ["region"] = "USA",
                        ["email"] = "a@b.com"
                    }
                })
        );
        // Same as above
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {"complex-flag": "decide-fallback-value"}}
            """
        );
        Assert.Equal(
            "decide-fallback-value",
            await client.GetFeatureFlagAsync(
                featureKey: "complex-flag",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new() { ["doesnt_matter"] = "1" }
                })
        );

        // this one will need to fall back
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {"complex-flag": "decide-fallback-value"}}
            """
        );
        Assert.Equal(
            "decide-fallback-value",
            await client.GetFeatureFlagAsync(
                featureKey: "complex-flag",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new() { ["region"] = "USA" }
                })
        );

        // Won't need to fallback
        Assert.False(
            await client.GetFeatureFlagAsync(
                featureKey: "complex-flag",
                distinctId: "some-distinct-id_outside_rollout?",
                options: new FeatureFlagOptions
                {
                    PersonProperties = new()
                    {
                        ["region"] = "USA",
                        ["email"] = "a@b.com",
                        ["name"] = "x",
                        ["doesnt_matter"] = "1"
                    }
                })
        );
    }

    [Fact] // Ported from PostHog/posthog-python test_feature_flags_fallback_to_decide
    public async Task CanFallbackToDecide()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":true,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 {
                                    "key":"id",
                                    "value":98,
                                    "operator": null,
                                    "type":"cohort"
                                 }
                              ],
                              "rollout_percentage":100
                           }
                        ]
                     }
                  },
                  {
                     "id":2,
                     "name":"Beta Feature",
                     "key":"beta-feature2",
                     "is_simple_flag":false,
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
            """
        );
        container.FakeHttpMessageHandler.AddRepeatedDecideResponse(
            count: 2,
            """
            {"featureFlags": {"beta-feature": "alakazam", "beta-feature2": "alakazam2"}}
            """
        );
        var client = container.Activate<PostHogClient>();

        // beta-feature fallbacks to decide because property type is unknown
        Assert.Equal("alakazam", await client.GetFeatureFlagAsync("beta-feature", "some-distinct-id"));

        // beta-feature2 fallbacks to decide because region property not given with call
        Assert.Equal("alakazam2", await client.GetFeatureFlagAsync("beta-feature2", "some-distinct-id"));
    }

    [Fact] // Ported from PostHog/posthog-python test_feature_flags_dont_fallback_to_decide_when_only_local_evaluation_is_true
    public async Task DoesNotFallbackToDecideWhenOnlyEvaluateLocallyIsTrue()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddRepeatedDecideResponse(
            count: 2,
            """
            {"featureFlags": {"beta-feature": "alakazam", "beta-feature2": "alakazam2"}}
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":true,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 {
                                    "key":"id",
                                    "value":98,
                                    "operator": null,
                                    "type":"cohort"
                                 }
                              ],
                              "rollout_percentage":100
                           }
                        ]
                     }
                  },
                  {
                     "id":2,
                     "name":"Beta Feature",
                     "key":"beta-feature2",
                     "is_simple_flag":false,
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
            """
        );
        var client = container.Activate<PostHogClient>();

        // beta-feature should fallback to decide because property type is unknown,
        // but doesn't because only_evaluate_locally is true
        Assert.Null(
            await client.GetFeatureFlagAsync(
                featureKey: "beta-feature",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions { OnlyEvaluateLocally = true })
        );
        Assert.Null(
            await client.IsFeatureEnabledAsync(
                featureKey: "beta-feature",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions { OnlyEvaluateLocally = true })
        );

        // beta-feature2 should fallback to decide because region property not given with call
        // but doesn't because only_evaluate_locally is true
        Assert.Null(
            await client.GetFeatureFlagAsync(
                featureKey: "beta-feature2",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions { OnlyEvaluateLocally = true })
        );
        Assert.Null(
            await client.IsFeatureEnabledAsync(
                featureKey: "beta-feature2",
                distinctId: "some-distinct-id",
                options: new FeatureFlagOptions { OnlyEvaluateLocally = true })
        );
    }

    [Fact] // Ported from PostHog/posthog-python test_get_feature_flag
    public async Task DoesNotCallDecideWhenCanBeEvaluatedLocally()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            { 
              "flags": [
                {
                    "id": 1,
                    "name": "Beta Feature",
                    "key": "beta-feature",
                    "is_simple_flag": false,
                    "active": true,
                    "rollout_percentage": 100,
                    "filters": {
                        "groups": [
                            {
                                "properties": [],
                                "rollout_percentage": 100
                            }
                        ],
                        "multivariate": {
                            "variants": [
                                {"key": "variant-1", "rollout_percentage": 50},
                                {"key": "variant-2", "rollout_percentage": 50}
                            ]
                        }
                    }
                }
              ]
            } 
            """
        );
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("beta-feature", distinctId: "some-distinct-Id");

        Assert.Equal(new FeatureFlag(Key: "beta-feature", IsEnabled: true, VariantKey: "variant-1"), result);
    }

    [Fact] // Ported from PostHog/posthog-python test_feature_flag_never_returns_undefined_during_regular_evaluation
    public async Task NeverReturnsNullDuringRegularEvaluation()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        var requestHandler = container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {}}
            """
        );
        var secondRequestHandler = container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {}}
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            { 
                "flags": [
                {
                    "id": 1,
                    "name": "Beta Feature",
                    "key": "beta-feature",
                    "is_simple_flag": true,
                    "active": true,
                    "filters": {
                        "groups": [
                            {
                                "properties": [],
                                "rollout_percentage": 0
                            }
                        ]
                    }
                }]
            } 
            """
        );
        var client = container.Activate<PostHogClient>();

        // beta-feature resolves to False, so no matter the default, stays False
        Assert.False(await client.GetFeatureFlagAsync("beta-feature", "some-distinct-id"));
        Assert.False(await client.IsFeatureEnabledAsync("beta-feature", "some-distinct-id"));
        Assert.Empty(requestHandler.ReceivedRequests);

        // beta-feature2 falls back to decide, and whatever decide returns is the value
        Assert.False(await client.GetFeatureFlagAsync("beta-feature2", "some-distinct-id"));
        Assert.False(await client.IsFeatureEnabledAsync("beta-feature2", "some-distinct-id"));
        Assert.Single(requestHandler.ReceivedRequests);
        Assert.Single(secondRequestHandler.ReceivedRequests);
    }

    [Fact] // Ported from PostHog/posthog-python test_feature_flag_return_none_when_decide_errors_out
    public async Task ReturnsNullWhenDecideThrowsException()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
#pragma warning disable CA2201
        var firstRequestHandler = container.FakeHttpMessageHandler.AddDecideResponseException(new Exception("Unknown error occurred"));
        var secondRequestHandler = container.FakeHttpMessageHandler.AddDecideResponseException(new Exception("Unknown error occurred"));
#pragma warning restore CA2201
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse("""{"flags":[]}""");
        var client = container.Activate<PostHogClient>();

        // beta-feature2 falls back to decide, which on error returns None
        Assert.Null(await client.GetFeatureFlagAsync("beta-feature2", "some-distinct-id"));
        Assert.Null(await client.IsFeatureEnabledAsync("beta-feature2", "some-distinct-id"));
        Assert.Single(firstRequestHandler.ReceivedRequests);
        Assert.Single(secondRequestHandler.ReceivedRequests);
    }

    [Fact] // Ported from PostHog/posthog-python test_experience_continuity_flag_not_evaluated_locally
    public async Task ExperienceContinuityFlagNotEvaluatedLocally()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {"beta-feature": "decide-fallback-value"}}
            """
        );
        var requestHandler = container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
                "flags": [
                    {
                        "id": 1,
                        "name": "Beta Feature",
                        "key": "beta-feature",
                        "is_simple_flag": false,
                        "active": true,
                        "rollout_percentage": 100,
                        "filters": {
                            "groups": [
                                {
                                    "properties": [],
                                    "rollout_percentage": 100
                                }
                            ]
                        },
                        "ensure_experience_continuity": true
                    }
                ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        // decide called always because experience_continuity is set
        Assert.Equal("decide-fallback-value", await client.GetFeatureFlagAsync("beta-feature", "distinct_id"));
        Assert.Single(requestHandler.ReceivedRequests);
    }

    [Fact] // Ported from PostHog/posthog-python test_feature_flags_local_evaluation_None_values
    public async Task LocalEvaluationWithNullValues()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":true,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "variant":"None",
                              "properties":[
                                 {
                                    "key":"latestBuildVersion",
                                    "type":"person",
                                    "value":".+",
                                    "operator":"regex"
                                 },
                                 {
                                    "key":"latestBuildVersionMajor",
                                    "type":"person",
                                    "value":"23",
                                    "operator":"gt"
                                 },
                                 {
                                    "key":"latestBuildVersionMinor",
                                    "type":"person",
                                    "value":"31",
                                    "operator":"gt"
                                 },
                                 {
                                    "key":"latestBuildVersionPatch",
                                    "type":"person",
                                    "value":"0",
                                    "operator":"gt"
                                 }
                              ],
                              "rollout_percentage":100
                           }
                        ]
                     }
                  }
               ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        var flag = await client.GetFeatureFlagAsync(
            featureKey: "beta-feature",
            distinctId: "some-distinct-id",
            options: new FeatureFlagOptions
            {
                PersonProperties = new()
                {
                    ["latestBuildVersion"] = null,
                    ["latestBuildVersionMajor"] = null,
                    ["latestBuildVersionMinor"] = null,
                    ["latestBuildVersionPatch"] = null
                }
            });

        Assert.False(flag);

        var anotherFlag = await client.GetFeatureFlagAsync(
            featureKey: "beta-feature",
            distinctId: "some-distinct-id",
            options: new FeatureFlagOptions
            {
                PersonProperties = new()
                {
                    ["latestBuildVersion"] = "24.32.1",
                    ["latestBuildVersionMajor"] = "24",
                    ["latestBuildVersionMinor"] = "32",
                    ["latestBuildVersionPatch"] = "1"
                }
            });
        Assert.True(anotherFlag);
    }

    [Fact]
    public async Task ReturnsFalseWhenFlagDoesNotExist()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddDecideResponse(
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = true
                }.AsReadOnly()
            });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("unknown-flag-key", "distinctId");

        Assert.False(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReturnsFlag(bool enabled)
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddDecideResponse(
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = enabled
                }.AsReadOnly()
            });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("flag-key", "distinct-id");

        Assert.NotNull(result);
        Assert.Equal(enabled, result.IsEnabled);
    }

    [Fact]
    public async Task ReturnsStringFlag()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddDecideResponse(
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = "premium-experience"
                }.AsReadOnly()
            });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("flag-key", "distinct-id");

        Assert.NotNull(result);
        Assert.Equal("premium-experience", result.VariantKey);
    }

    [Fact]
    public async Task CapturesFeatureFlagCalledEvent()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedDecideResponse(
            count: 2,
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = true
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("flag-key", "a-distinct-id");
        // This call should not call capture because same key, distinct-id, and result.
        await client.GetFeatureFlagAsync("flag-key", "a-distinct-id");

        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        await client.FlushAsync();
        var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                       {
                         "api_key": "fake-project-api-key",
                         "historical_migrations": false,
                         "batch": [
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": true,
                               "locally_evaluated": false,
                               "$feature/flag-key": true,
                               "distinct_id": "a-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:23\u002B00:00"
                           }
                         ]
                       }
                       """, received);
    }

    [Fact]
    public async Task CapturesFeatureFlagCalledEventAgainIfCacheLimitExceededAndIsCompacted()
    {
        var container = new TestContainer(sp =>
        {
            sp.Configure<PostHogOptions>(options =>
            {
                options.ProjectApiKey = "fake-project-api-key";
                options.FeatureFlagSentCacheSizeLimit = 2;
                options.FeatureFlagSentCacheCompactionPercentage = .5; // 50%, or 1 item.
            });
        });
        var timeProvider = container.FakeTimeProvider;
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedDecideResponse(
            count: 6,
            new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"flag-variant-1",
                    ["another-flag-key"] = $"flag-variant-2"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        // This is captured and the cache entry will be compacted when the size limit exceeded.
        await client.GetFeatureFlagAsync("flag-key", "a-distinct-id"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("flag-key", "another-distinct-id"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-flag-key", "another-distinct-id"); // Captured, cache compaction will occur after this.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-flag-key", "another-distinct-id"); // Cache hit, not captured.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("flag-key", "a-distinct-id"); // Captured because cache limit exceeded.

        await client.FlushAsync();
        var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                       {
                         "api_key": "fake-project-api-key",
                         "historical_migrations": false,
                         "batch": [
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": "flag-variant-1",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-1",
                               "distinct_id": "a-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:23\u002B00:00"
                           },
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": "flag-variant-1",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-1",
                               "distinct_id": "another-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:24\u002B00:00"
                           },
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "another-flag-key",
                               "$feature_flag_response": "flag-variant-2",
                               "locally_evaluated": false,
                               "$feature/another-flag-key": "flag-variant-2",
                               "distinct_id": "another-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:25\u002B00:00"
                           },
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": "flag-variant-1",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-1",
                               "distinct_id": "a-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:27\u002B00:00"
                           }
                         ]
                       }
                       """, received);
    }

    [Fact]
    public async Task CapturesFeatureFlagCalledEventAgainIfCacheSlidingWindowExpirationOccurs()
    {
        var container = new TestContainer(sp => sp.AddSingleton<IOptions<PostHogOptions>>(new PostHogOptions
        {
            ProjectApiKey = "test-api-key",
            FeatureFlagSentCacheSizeLimit = 20,
            FeatureFlagSentCacheSlidingExpiration = TimeSpan.FromSeconds(3)
        }));
        var timeProvider = container.FakeTimeProvider;
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedDecideResponse(
            count: 6,
            new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"flag-variant-1",
                    ["another-flag-key"] = $"flag-variant-2"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        await client.GetFeatureFlagAsync("flag-key", "a-distinct-id"); // Captured.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("flag-key", "another-distinct-id"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-flag-key", "another-distinct-id"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1)); // Sliding time window expires for first entry.
        await client.GetFeatureFlagAsync("another-flag-key", "another-distinct-id"); // Cache hit, not captured.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("flag-key", "a-distinct-id"); // Captured.

        await client.FlushAsync();
        var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                       {
                         "api_key": "test-api-key",
                         "historical_migrations": false,
                         "batch": [
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": "flag-variant-1",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-1",
                               "distinct_id": "a-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:23\u002B00:00"
                           },
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": "flag-variant-1",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-1",
                               "distinct_id": "another-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:24\u002B00:00"
                           },
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "another-flag-key",
                               "$feature_flag_response": "flag-variant-2",
                               "locally_evaluated": false,
                               "$feature/another-flag-key": "flag-variant-2",
                               "distinct_id": "another-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:25\u002B00:00"
                           },
                           {
                             "event": "$feature_flag_called",
                             "properties": {
                               "$feature_flag": "flag-key",
                               "$feature_flag_response": "flag-variant-1",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-1",
                               "distinct_id": "a-distinct-id",
                               "$lib": "posthog-dotnet",
                               "$lib_version": "{{client.Version}}",
                               "$geoip_disable": true
                             },
                             "timestamp": "2024-01-21T19:08:27\u002B00:00"
                           }
                         ]
                       }
                       """, received);
    }

    [Fact]
    public async Task DoesNotCaptureFeatureFlagCalledEventWhenSendFeatureFlagsFalse()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedDecideResponse(
            count: 4,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"feature-value-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        await client.GetFeatureFlagAsync(featureKey: "flag-key",
            distinctId: "a-distinct-id", options: new FeatureFlagOptions { SendFeatureFlagEvents = false });

        await client.FlushAsync();
        Assert.Empty(captureRequestHandler.ReceivedRequests);
    }
}

public class TheGetAllFeatureFlagsAsyncMethod
{
    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_with_fallback
    public async Task RetrievesAllFlagsWithFallback()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {
               "featureFlags":{
                  "beta-feature":"variant-1",
                  "beta-feature2":"variant-2",
                  "disabled-feature":false
               }
            }
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
              "flags":[
                 {
                    "id":1,
                    "name":"Beta Feature",
                    "key":"beta-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "rollout_percentage":100,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":100
                          }
                       ]
                    }
                 },
                 {
                    "id":2,
                    "name":"Beta Feature",
                    "key":"disabled-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":0
                          }
                       ]
                    }
                 },
                 {
                    "id":3,
                    "name":"Beta Feature",
                    "key":"beta-feature2",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[
                                {
                                   "key":"country",
                                   "value":"US"
                                }
                             ],
                             "rollout_percentage":0
                          }
                       ]
                    }
                 }
              ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        // We expect a fallback because no properties were supplied.
        var results = await client.GetAllFeatureFlagsAsync(distinctId: "some-distinct-id");

        // beta-feature value overridden by /decide
        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true, VariantKey: "variant-1"),
            ["beta-feature2"] = new(Key: "beta-feature2", IsEnabled: true, VariantKey: "variant-2"),
            ["disabled-feature"] = new(Key: "disabled-feature", IsEnabled: false)
        }, results);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_and_payloads_with_fallback
    public async Task RetrievesAllFlagsAndPayloadsWithFallback()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        var captureRequestHandler = container.FakeHttpMessageHandler.AddBatchResponse();
        var decideRequestHandler = container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {
               "featureFlags":{
                  "beta-feature":"variant-1",
                  "beta-feature2":"variant-2",
                  "disabled-feature":false
               },
               "featureFlagPayloads": {"beta-feature": "100", "beta-feature2": "300"}
            }
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "rollout_percentage":100,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":100
                           }
                        ],
                        "payloads":{
                           "true":"some-payload"
                        }
                     }
                  },
                  {
                     "id":2,
                     "name":"Beta Feature",
                     "key":"disabled-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":0
                           }
                        ],
                        "payloads":{
                           "true":"another-payload"
                        }
                     }
                  },
                  {
                     "id":3,
                     "name":"Beta Feature",
                     "key":"beta-feature2",
                     "is_simple_flag":false,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 {
                                    "key":"country",
                                    "value":"US"
                                 }
                              ],
                              "rollout_percentage":0
                           }
                        ],
                        "payloads":{
                           "true":"payload-3"
                        }
                     }
                  }
               ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        // We expect a fallback because no properties were supplied.
        var results = await client.GetAllFeatureFlagsAsync(distinctId: "some-distinct-id");

        // beta-feature value overridden by /decide
        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true, VariantKey: "variant-1", Payload: "100"),
            ["beta-feature2"] = new(Key: "beta-feature2", IsEnabled: true, VariantKey: "variant-2", Payload: "300"),
            ["disabled-feature"] = new(Key: "disabled-feature", IsEnabled: false)
        }, results);
        Assert.Single(decideRequestHandler.ReceivedRequests);
        Assert.Empty(captureRequestHandler.ReceivedRequests);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_with_fallback_empty_local_flags
    public async Task RetrievesAllFlagsWithFallbackAndEmptyLocalFlags()
    {
        var container = new TestContainer(personalApiKey: "fake-person");
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {"featureFlags": {"beta-feature": "variant-1", "beta-feature2": "variant-2"}}
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {"flags": []}
            """
        );
        var client = container.Activate<PostHogClient>();

        // Beta feature overridden by /decide
        var result = await client.GetAllFeatureFlagsAsync("some-distinct-id");

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true, VariantKey: "variant-1"),
            ["beta-feature2"] = new(Key: "beta-feature2", IsEnabled: true, VariantKey: "variant-2")
        }, result);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_and_payloads_with_fallback_empty_local_flags
    public async Task RetrievesAllFlagsAndPayloadsWithFallbackAndEmptyLocalFlags()
    {
        var container = new TestContainer(personalApiKey: "fake-person");
        container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {
                "featureFlags": {"beta-feature": "variant-1", "beta-feature2": "variant-2"},
                "featureFlagPayloads": {"beta-feature": "100", "beta-feature2": "300"}
            }
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {"flags": []}
            """
        );
        var client = container.Activate<PostHogClient>();

        // Beta feature overridden by /decide
        var result = await client.GetAllFeatureFlagsAsync("some-distinct-id");

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true, VariantKey: "variant-1", Payload: "100"),
            ["beta-feature2"] = new(Key: "beta-feature2", IsEnabled: true, VariantKey: "variant-2", Payload: "300")
        }, result);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_with_no_fallback
    public async Task RetrievesAllFlagsWithNoFallback()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
              "flags":[
                 {
                    "id":1,
                    "name":"Beta Feature",
                    "key":"beta-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "rollout_percentage":100,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":100
                          }
                       ]
                    }
                 },
                 {
                    "id":2,
                    "name":"Beta Feature",
                    "key":"disabled-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":0
                          }
                       ]
                    }
                 }
              ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        var results = await client.GetAllFeatureFlagsAsync("some-distinct-id");

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new("beta-feature", true),
            ["disabled-feature"] = new("disabled-feature", false)
        }, results);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_and_payloads_with_no_fallback
    public async Task RetrievesAllFlagsAndPayloadsWithNoFallback()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "rollout_percentage":100,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":100
                           }
                        ],
                        "payloads":{
                           "true":"new"
                        }
                     }
                  },
                  {
                     "id":2,
                     "name":"Beta Feature",
                     "key":"disabled-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":0
                           }
                        ],
                        "payloads":{
                           "true":"some-payload"
                        }
                     }
                  }
               ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        var results = await client.GetAllFeatureFlagsAsync("some-distinct-id");

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new("beta-feature", true, Payload: "new"),
            ["disabled-feature"] = new("disabled-feature", false)
        }, results);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_with_fallback_but_only_local_evaluation_set
    public async Task RetrievesAllFlagsWithFallbackButOnlyLocalEvaluationSet()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        var decideHandler = container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {
               "featureFlags":{
                  "beta-feature":"variant-1",
                  "beta-feature2":"variant-2"
               }
            }
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
              "flags":[
                 {
                    "id":1,
                    "name":"Beta Feature",
                    "key":"beta-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "rollout_percentage":100,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":100
                          }
                       ]
                    }
                 },
                 {
                    "id":2,
                    "name":"Beta Feature",
                    "key":"disabled-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":0
                          }
                       ]
                    }
                 },
                 {
                    "id":3,
                    "name":"Beta Feature",
                    "key":"beta-feature2",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[
                                {
                                   "key":"country",
                                   "value":"US"
                                }
                             ],
                             "rollout_percentage":0
                          }
                       ]
                    }
                 }
              ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        // We expect a fallback because no properties were supplied.
        var results = await client.GetAllFeatureFlagsAsync(
            distinctId: "some-distinct-id",
            options: new AllFeatureFlagsOptions { OnlyEvaluateLocally = true });

        // beta-feature2 has no value
        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true),
            ["disabled-feature"] = new(Key: "disabled-feature", IsEnabled: false)
        }, results);
        Assert.Empty(decideHandler.ReceivedRequests);
    }

    [Fact] // Ported from PostHog/posthog-python test_get_all_flags_and_payloads_with_fallback_but_only_local_evaluation_set
    public async Task RetrievesAllFlagsAndPayloadsWithFallbackButOnlyLocalEvaluationSet()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        var decideHandler = container.FakeHttpMessageHandler.AddDecideResponse(
            """
            {
                "featureFlags": {"beta-feature": "variant-1", "beta-feature2": "variant-2"},
                "featureFlagPayloads": {"beta-feature": "100", "beta-feature2": "300"}
            }
            """
        );
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
              "flags":[
                 {
                    "id":1,
                    "name":"Beta Feature",
                    "key":"beta-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "rollout_percentage":100,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":100
                          }
                       ],
                       "payloads": {
                            "true": "some-payload"
                       }
                    }
                 },
                 {
                    "id":2,
                    "name":"Beta Feature",
                    "key":"disabled-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":0
                          }
                       ],
                       "payloads": {
                            "true": "another-payload"
                       }
                    }
                 },
                 {
                    "id":3,
                    "name":"Beta Feature",
                    "key":"beta-feature2",
                    "is_simple_flag":false,
                    "active":true,
                    "filters":{
                       "groups":[
                          {
                             "properties":[
                                {
                                   "key":"country",
                                   "value":"US"
                                }
                             ],
                             "rollout_percentage":0
                          }
                       ],
                       "payloads": {
                            "true": "payload-3"
                       }
                    }
                 }
              ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        // We expect a fallback because no properties were supplied.
        var results = await client.GetAllFeatureFlagsAsync(
            distinctId: "some-distinct-id",
            options: new AllFeatureFlagsOptions { OnlyEvaluateLocally = true });

        // beta-feature2 has no value
        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true, Payload: "some-payload"),
            ["disabled-feature"] = new(Key: "disabled-feature", IsEnabled: false)
        }, results);
        Assert.Empty(decideHandler.ReceivedRequests);
    }

    [Fact] // Ported from PostHog/posthog-python test_compute_inactive_flags_locally
    public async Task ComputesInactiveFlagsLocally()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "rollout_percentage":100,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":100
                           }
                        ]
                     }
                  },
                  {
                     "id":2,
                     "name":"Beta Feature",
                     "key":"disabled-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":0
                           }
                        ]
                     }
                  }
               ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: true),
            ["disabled-feature"] = new(Key: "disabled-feature", IsEnabled: false)
        }, await client.GetAllFeatureFlagsAsync("some-distinct-id"));

        // Now, after a poll interval, flag 1 is inactive, and flag 2 rollout is set to 100%.
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
               "flags":[
                  {
                     "id":1,
                     "name":"Beta Feature",
                     "key":"beta-feature",
                     "is_simple_flag":false,
                     "active":false,
                     "rollout_percentage":100,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":100
                           }
                        ]
                     }
                  },
                  {
                     "id":2,
                     "name":"Beta Feature",
                     "key":"disabled-feature",
                     "is_simple_flag":false,
                     "active":true,
                     "filters":{
                        "groups":[
                           {
                              "properties":[
                                 
                              ],
                              "rollout_percentage":100
                           }
                        ]
                     }
                  }
               ]
            }
            """
        );
        container.FakeTimeProvider.Advance(TimeSpan.FromMinutes(1));
        await Task.Delay(1); // Cede execution to thread that's loading the new flags.

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new(Key: "beta-feature", IsEnabled: false),
            ["disabled-feature"] = new(Key: "disabled-feature", IsEnabled: true)
        }, await client.GetAllFeatureFlagsAsync("some-distinct-id"));
    }

    [Fact]
    public async Task RetrievesFlagFromHttpContextCacheOnSecondCall()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var cache = new HttpContextFeatureFlagCache(httpContextAccessor);
        var options = new FakeOptions<PostHogOptions>(new PostHogOptions());
        var apiClient = Substitute.For<IPostHogApiClient>();
        // Set up a call to the API client that returns all feature flags
        apiClient.GetAllFeatureFlagsFromDecideAsync(
                distinctUserId: "1234",
                personProperties: null,
                groupProperties: null,
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(
                _ => new DecideApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = true,
                        ["another-flag-key"] = "some-value",
                    }.AsReadOnly()
                },
                _ => throw new InvalidOperationException("Called more than once")
            );

        await using var client = new PostHogClient(
            apiClient,
            cache,
            options,
            new FakeTaskScheduler(),
            new FakeTimeProvider(),
            NullLogger<PostHogClient>.Instance);

        var flags = await client.GetAllFeatureFlagsAsync(distinctId: "1234");
        var flagsAgain = await client.GetAllFeatureFlagsAsync(distinctId: "1234");
        var firstFlag = await client.GetFeatureFlagAsync("flag-key", "1234");

        Assert.NotEmpty(flags);
        Assert.Same(flags, flagsAgain);
        Assert.NotNull(firstFlag);
        Assert.Equal("flag-key", firstFlag.Key);
    }
}
