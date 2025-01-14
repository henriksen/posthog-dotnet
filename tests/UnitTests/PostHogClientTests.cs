using System.Collections.ObjectModel;
using NSubstitute;
using PostHog;
using PostHog.Features;

public class FeatureFlagExtensionsTests
{
    public class TheGetFeatureFlagAsyncMethod
    {
        [Fact]
        public async Task ReturnsUndefinedWhenFlagDoesNotExist()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync("distinctId", Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(new Dictionary<string, FeatureFlag>()));

            var result = await client.GetFeatureFlagAsync(
                "distinctId",
                "flag-key",
                new Dictionary<string, object>(),
                CancellationToken.None);

            Assert.Null(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReturnsFlag(bool enabled)
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync("distinctId", Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", enabled, null, null)
                    }));

            var result = await client.GetFeatureFlagAsync(
                "distinctId",
                "flag-key",
                new Dictionary<string, object>(),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(enabled, result.IsEnabled);
        }

        [Fact]
        public async Task ReturnsStringFlag()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync("distinctId", Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", true, "premium-experience", null)
                    }));

            var result = await client.GetFeatureFlagAsync(
                "distinctId",
                "flag-key",
                new Dictionary<string, object>(),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("premium-experience", result.VariantKey);
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
            client.GetFeatureFlagsAsync("distinctId", Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", enabled, null, null)
                    }));

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "flag-key",
                new Dictionary<string, object>(),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(enabled, result.Value);
        }

        [Fact]
        public async Task ReturnsTrueWhenFlagReturnsString()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync("distinctId", Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(
                    new Dictionary<string, FeatureFlag>
                    {
                        ["flag-key"] = new FeatureFlag("flag-key", true, "premium-experience", null)
                    }));

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "flag-key",
                new Dictionary<string, object>(),
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result);
        }

        [Fact]
        public async Task ReturnsNullWhenFlagDoesNotExist()
        {
            var client = Substitute.For<IPostHogClient>();
            client.GetFeatureFlagsAsync("distinctId", Arg.Any<CancellationToken>())
                .Returns(new ReadOnlyDictionary<string, FeatureFlag>(new Dictionary<string, FeatureFlag>()));

            var result = await client.IsFeatureEnabledAsync(
                "distinctId",
                "missing-flag-key",
                new Dictionary<string, object>(),
                CancellationToken.None);

            Assert.Null(result);
        }
    }
}