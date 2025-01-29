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
    public async Task CapturesFeatureFlagCalledEventOnlyOncePerDistinctIdAndFlagKey()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedResponses(4,
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"feature-value-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.Activate<PostHogClient>();

        await client.IsFeatureEnabledAsync("a-distinct-id", "flag-key");
        await client.IsFeatureEnabledAsync("a-distinct-id", "flag-key"); // Cache hit, not captured.
        await client.IsFeatureEnabledAsync("another-distinct-id", "flag-key");

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
                               "$feature_flag_response": "feature-value-0",
                               "locally_evaluated": false,
                               "$feature/flag-key": "feature-value-0",
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
                               "$feature_flag_response": "feature-value-2",
                               "locally_evaluated": false,
                               "$feature/flag-key": "feature-value-2",
                               "distinct_id": "another-distinct-id",
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
    public async Task DoesNotCaptureFeatureFlagCalledEventWhenSendFeatureFlagsFalse()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedResponses(4,
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"feature-value-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.Activate<PostHogClient>();

        await client.IsFeatureEnabledAsync(
            distinctId: "a-distinct-id",
            featureKey: "flag-key",
            options: new FeatureFlagOptions { SendFeatureFlagEvents = false });

        await client.FlushAsync();
        Assert.Empty(captureRequestHandler.ReceivedRequests);
    }
}

public class TheGetFeatureFlagAsyncMethod
{
    [Fact]
    public async Task ReturnsUndefinedWhenFlagDoesNotExist()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = true
                }.AsReadOnly()
            });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("distinctId", "unknown-flag-key");

        Assert.Null(result);
        Assert.Equal("undefined", result.ToResponseObject());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReturnsFlag(bool enabled)
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = enabled
                }.AsReadOnly()
            });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("distinct-id", "flag-key");

        Assert.NotNull(result);
        Assert.Equal(enabled, result.IsEnabled);
    }

    [Fact]
    public async Task ReturnsStringFlag()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = "premium-experience"
                }.AsReadOnly()
            });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("distinct-id", "flag-key");

        Assert.NotNull(result);
        Assert.Equal("premium-experience", result.VariantKey);
    }

    [Fact]
    public async Task CapturesFeatureFlagCalledEvent()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            responseBody: new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = true
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.Activate<PostHogClient>();

        var result = await client.GetFeatureFlagAsync("a-distinct-id", "flag-key");

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
            sp.AddSingleton<IOptions<PostHogOptions>>(new PostHogOptions
            {
                ProjectApiKey = "test-api-key",
                FeatureFlagSentCacheSizeLimit = 2,
                FeatureFlagSentCacheCompactionPercentage = .5 // 50%, or 1 item.
            });
        });
        var timeProvider = container.FakeTimeProvider;
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddRepeatedResponses(6,
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"flag-variant-{count}",
                    ["another-flag-key"] = $"flag-variant-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.Activate<PostHogClient>();

        // This is captured and the cache entry will be compacted when the size limit exceeded.
        await client.GetFeatureFlagAsync("a-distinct-id", "flag-key"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-distinct-id", "flag-key"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-distinct-id", "another-flag-key"); // Captured, cache compaction will occur after this.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-distinct-id", "another-flag-key"); // Cache hit, not captured.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("a-distinct-id", "flag-key"); // Captured because cache limit exceeded.

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
                               "$feature_flag_response": "flag-variant-0",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-0",
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
                               "$feature_flag_response": "flag-variant-4",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-4",
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
        messageHandler.AddRepeatedResponses(6,
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"flag-variant-{count}",
                    ["another-flag-key"] = $"flag-variant-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.Activate<PostHogClient>();

        await client.GetFeatureFlagAsync("a-distinct-id", "flag-key"); // Captured.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-distinct-id", "flag-key"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("another-distinct-id", "another-flag-key"); // Captured
        timeProvider.Advance(TimeSpan.FromSeconds(1)); // Sliding time window expires for first entry.
        await client.GetFeatureFlagAsync("another-distinct-id", "another-flag-key"); // Cache hit, not captured.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await client.GetFeatureFlagAsync("a-distinct-id", "flag-key"); // Captured.

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
                               "$feature_flag_response": "flag-variant-0",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-0",
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
                               "$feature_flag_response": "flag-variant-4",
                               "locally_evaluated": false,
                               "$feature/flag-key": "flag-variant-4",
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
        messageHandler.AddRepeatedResponses(4,
            new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            count => new DecideApiResult
            {
                FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                {
                    ["flag-key"] = $"feature-value-{count}"
                }.AsReadOnly()
            });
        var captureRequestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.Activate<PostHogClient>();

        await client.GetFeatureFlagAsync(
            distinctId: "a-distinct-id",
            featureKey: "flag-key",
            options: new FeatureFlagOptions { SendFeatureFlagEvents = false });

        await client.FlushAsync();
        Assert.Empty(captureRequestHandler.ReceivedRequests);
    }
}

public class TheGetAllFeatureFlagsAsyncMethod
{
    [Fact] // Ported from test_get_all_flags_with_fallback
    public async Task RetrievesAllFlagsWithFallback()
    {
        var decideJson = """
                         {
                            "featureFlags":{
                               "beta-feature":"variant-1",
                               "beta-feature2":"variant-2",
                               "disabled-feature":false
                            }
                         }
                         """;
        var localJson = """
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
                   """;
        var container = new TestContainer(services =>
        {
            services.Configure<PostHogOptions>(options =>
            {
                options.ProjectApiKey = "fake-project-api-key";
                options.PersonalApiKey = "fake-personal-api-key";
            });
        });
        container.FakeHttpMessageHandler.AddResponse(new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            responseBody: await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<DecideApiResult>(decideJson));
        container.FakeHttpMessageHandler.AddResponse(new Uri("https://us.i.posthog.com/api/feature_flag/local_evaluation/?token=fake-project-api-key?send_cohorts"),
            HttpMethod.Get,
            responseBody: await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<LocalEvaluationApiResult>(localJson));
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

    [Fact] // Ported from test_get_all_flags_with_fallback
    public async Task RetrievesAllFlagsWithFallbackButOnlyLocalEvaluationSet()
    {
        var decideJson = """
                         {
                            "featureFlags":{
                               "beta-feature":"variant-1",
                               "beta-feature2":"variant-2"
                            }
                         }
                         """;
        var localJson = """
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
                   """;
        var container = new TestContainer(services =>
        {
            services.Configure<PostHogOptions>(options =>
            {
                options.ProjectApiKey = "fake-project-api-key";
                options.PersonalApiKey = "fake-personal-api-key";
            });
        });
        container.FakeHttpMessageHandler.AddResponse(new Uri("https://us.i.posthog.com/decide?v=3"),
            HttpMethod.Post,
            responseBody: await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<DecideApiResult>(decideJson));
        container.FakeHttpMessageHandler.AddResponse(new Uri("https://us.i.posthog.com/api/feature_flag/local_evaluation/?token=fake-project-api-key?send_cohorts"),
            HttpMethod.Get,
            responseBody: await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<LocalEvaluationApiResult>(localJson));
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
    }

    [Fact] // Ported from test_get_all_flags_with_no_fallback
    public async Task RetrievesAllFlagsWithNoFallback()
    {
        var json = """
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
                   """;
        var container = new TestContainer(services =>
        {
            services.Configure<PostHogOptions>(options =>
            {
                options.ProjectApiKey = "fake-project-api-key";
                options.PersonalApiKey = "fake-personal-api-key";
            });
        });
        container.FakeHttpMessageHandler.AddResponse(new Uri("https://us.i.posthog.com/api/feature_flag/local_evaluation/?token=fake-project-api-key?send_cohorts"),
            HttpMethod.Get,
            responseBody: await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<LocalEvaluationApiResult>(json));
        var client = container.Activate<PostHogClient>();

        var results = await client.GetAllFeatureFlagsAsync("some-distinct-id");

        Assert.Equal(new Dictionary<string, FeatureFlag>
        {
            ["beta-feature"] = new("beta-feature", true),
            ["disabled-feature"] = new("disabled-feature", false)
        }, results);
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
        var firstFlag = await client.GetFeatureFlagAsync("1234", "flag-key");

        Assert.NotEmpty(flags);
        Assert.Same(flags, flagsAgain);
        Assert.NotNull(firstFlag);
        Assert.Equal("flag-key", firstFlag.Key);
    }
}
