using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PostHog;
using PostHog.Api;
using PostHog.Features;
using PostHog.Json;
using UnitTests.Fakes;

#pragma warning disable CA2000
namespace FeatureFlagExtensionsTests;

public class TheIsFeatureEnabledAsyncMethod
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReturnsFlagResult(bool enabled)
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
