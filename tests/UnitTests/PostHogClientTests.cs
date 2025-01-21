using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PostHog;
using PostHog.Api;
using PostHog.Cache;
using PostHog.Config;
using PostHog.Json;

namespace UnitTests;

public class PostHogClientTests
{
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
            apiClient.GetFeatureFlagsAsync(
                    distinctUserId: "1234",
                    groups: null,
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(
                    _ => new FeatureFlagsApiResult
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
                new FakeTimeProvider(),
                NullLogger<PostHogClient>.Instance);

            var flags = await client.GetFeatureFlagsAsync(
                distinctId: "1234",
                groups: null,
                personProperties: null,
                groupProperties: null,
                CancellationToken.None);
            var flagsAgain = await client.GetFeatureFlagsAsync(
                distinctId: "1234",
                groups: null,
                personProperties: null,
                groupProperties: null,
                CancellationToken.None);
            var firstFlag = await client.GetFeatureFlagAsync("1234", "flag-key");

            Assert.NotEmpty(flags);
            Assert.Same(flags, flagsAgain);
            Assert.NotNull(firstFlag);
            Assert.Equal("flag-key", firstFlag.Key);
        }
    }
}