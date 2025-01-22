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

