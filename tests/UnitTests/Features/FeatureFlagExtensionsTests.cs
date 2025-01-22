using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PostHog;
using PostHog.Api;
using PostHog.Config;
using PostHog.Features;
using PostHog.Json;

#pragma warning disable CA2000
public class FeatureFlagExtensionsTests
{
    static PostHogClient SetUpPostHogClient(out FakeHttpMessageHandler messageHandler)
    {
        messageHandler = new FakeHttpMessageHandler();
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var httpClient = new HttpClient(messageHandler);
        var options = new PostHogOptions { ProjectApiKey = "test" };
        var apiClient = new PostHogApiClient(
            httpClient,
            options,
            timeProvider,
            new NullLogger<PostHogApiClient>());
        return new PostHogClient(
            apiClient,
            NullFeatureFlagCache.Instance,
            options,
            new FakeTaskScheduler(),
            timeProvider,
            new NullLogger<PostHogClient>());
    }

    public class TheIsFeatureEnabledAsyncMethod
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReturnsFlagResult(bool enabled)
        {
            var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = enabled
                    }.AsReadOnly()
                });

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
            var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = "premium-experience"
                    }.AsReadOnly()
                });

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "flag-key",
                CancellationToken.None);

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

