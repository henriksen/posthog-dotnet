using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PostHog;
using PostHog.Api;
using PostHog.Config;
using PostHog.Features;
using PostHog.Json;

public class FeatureFlagExtensionsTests
{
    public class TheGetFeatureFlagAsyncMethod
    {
        [Fact]
        public async Task ReturnsUndefinedWhenFlagDoesNotExist()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync(
                    distinctId: "distinctId",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(new Dictionary<string, FeatureFlag>()));

            var result = await client.GetFeatureFlagAsync(
                "distinctId",
                "flag-key",
                CancellationToken.None);

            Assert.Null(result);
            Assert.Equal("undefined", result.ToResponseObject());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReturnsFlag(bool enabled)
        {
            var client = Substitute.For<IPostHogClient>();

            client.GetFeatureFlagsAsync(
                    distinctId: "distinct-id",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", enabled, null, null)
                    }));

            var result = await client.GetFeatureFlagAsync(
                "distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(enabled, result.IsEnabled);
        }

        [Fact]
        public async Task ReturnsStringFlag()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync(
                    distinctId: "distinct-id",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", true, "premium-experience", null)
                    }));

            var result = await client.GetFeatureFlagAsync(
                "distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("premium-experience", result.VariantKey);
        }

        [Fact]
        public async Task CapturesFeatureFlagCalledEvent()
        {
            using var messageHandler = new FakeHttpMessageHandler();
            var timeProvider = new FakeTimeProvider();
            timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
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
            using var httpClient = new HttpClient(messageHandler);
            var options = new PostHogOptions { ProjectApiKey = "test" };
            using var apiClient = new PostHogApiClient(
                httpClient,
                options,
                timeProvider,
                new NullLogger<PostHogApiClient>());
            await using var client = new PostHogClient(
                apiClient,
                NullFeatureFlagCache.Instance,
                options,
                timeProvider,
                new NullLogger<PostHogClient>());

            var result = await client.GetFeatureFlagAsync(
                "a-distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsEnabled);
            await client.FlushAsync();
            var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
            Assert.Equal($$"""
                           {
                             "api_key": "test",
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
    }

    public class TheIsFeatureEnabledAsyncMethod
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReturnsFlagResult(bool enabled)
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync(
                    distinctId: "distinctId",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", enabled, null, null)
                    }));

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(enabled, result.Value);
        }

        [Fact]
        public async Task ReturnsTrueWhenFlagReturnsString()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync(
                    distinctId: "distinctId",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", true, "premium-experience", null)
                    }));

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsNullWhenFlagDoesNotExist()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync(
                    distinctId: "distinctId",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(new Dictionary<string, FeatureFlag>()));

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "missing-flag-key",
                CancellationToken.None);

            Assert.Null(result);
        }
    }
}

