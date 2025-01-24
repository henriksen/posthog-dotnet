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
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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

        var result = await client.GetFeatureFlagAsync("distinctId", "unknown-flag-key");

        Assert.Null(result);
        Assert.Equal("undefined", result.ToResponseObject());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReturnsFlag(bool enabled)
    {
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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

        var result = await client.GetFeatureFlagAsync("distinct-id", "flag-key");

        Assert.NotNull(result);
        Assert.Equal(enabled, result.IsEnabled);
    }

    [Fact]
    public async Task ReturnsStringFlag()
    {
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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

        var result = await client.GetFeatureFlagAsync("distinct-id", "flag-key");

        Assert.NotNull(result);
        Assert.Equal("premium-experience", result.VariantKey);
    }

    [Fact]
    public async Task CapturesFeatureFlagCalledEvent()
    {
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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
        var container = new FakeContainer();
        var timeProvider = container.GetRequiredService<FakeTimeProvider>();
        container.AddService<IOptions<PostHogOptions>>(new PostHogOptions
        {
            ProjectApiKey = "test-api-key",
            FeatureFlagSentCacheSizeLimit = 2,
            FeatureFlagSentCacheCompactionPercentage = .5 // 50%, or 1 item.
        });
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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
        var container = new FakeContainer();
        var timeProvider = container.GetRequiredService<FakeTimeProvider>();
        container.AddService<IOptions<PostHogOptions>>(new PostHogOptions
        {
            ProjectApiKey = "test-api-key",
            FeatureFlagSentCacheSizeLimit = 20,
            FeatureFlagSentCacheSlidingExpiration = TimeSpan.FromSeconds(3)
        });
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var client = container.GetRequiredService<PostHogClient>();
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

        await client.GetFeatureFlagAsync(
            distinctId: "a-distinct-id",
            featureKey: "flag-key",
            options: new FeatureFlagOptions { SendFeatureFlagEvents = false });

        await client.FlushAsync();
        Assert.Empty(captureRequestHandler.ReceivedRequests);
    }
}

public class TheGetFeatureFlagsAsyncMethod
{
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
        apiClient.GetFeatureFlagsFromDecideAsync(
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

        var flags = await client.GetFeatureFlagsAsync(distinctId: "1234");
        var flagsAgain = await client.GetFeatureFlagsAsync(
            distinctId: "1234",
            personProperties: null,
            groupProperties: null);
        var firstFlag = await client.GetFeatureFlagAsync("1234", "flag-key");

        Assert.NotEmpty(flags);
        Assert.Same(flags, flagsAgain);
        Assert.NotNull(firstFlag);
        Assert.Equal("flag-key", firstFlag.Key);
    }
}
