using Microsoft.AspNetCore.Http;
using NSubstitute;
using PostHog.Cache;
using PostHog.Features;

public class HttpContextFeatureFlagCacheTests
{
    public class TheGetAndCacheFeatureFlagsAsyncMethod
    {
        [Fact]
        public async Task CachesFlagsInHttpContext()
        {
            // Arrange
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContextAccessor.HttpContext.Returns(httpContext);

            var cache = new HttpContextFeatureFlagCache(httpContextAccessor);
            var distinctId = "user123";
            var featureFlags = new Dictionary<string, FeatureFlag>
            {
                { "feature1", new FeatureFlag(Key: "feature1", IsEnabled: true, null, null) }
            };

            Func<CancellationToken, Task<IReadOnlyDictionary<string, FeatureFlag>>> fetcher = _ =>
                Task.FromResult((IReadOnlyDictionary<string, FeatureFlag>)featureFlags);

            // Act
            var result = await cache.GetAndCacheFeatureFlagsAsync(distinctId, fetcher, CancellationToken.None);

            // Assert
            Assert.Equal(featureFlags, result);
            Assert.Equal(featureFlags, httpContext.Items[$"$PostHog(feature_flags):{distinctId}"]);
        }

        [Fact]
        public async Task ReturnsCachedFlagsFromHttpContext()
        {
            // Arrange
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            var distinctId = "user123";
            var cachedFeatureFlags = new Dictionary<string, FeatureFlag>
            {
                { "feature1", new FeatureFlag(Key: "feature1", IsEnabled: true, null, null) }
            };
            httpContext.Items[$"$PostHog(feature_flags):{distinctId}"] = cachedFeatureFlags;
            httpContextAccessor.HttpContext.Returns(httpContext);

            var cache = new HttpContextFeatureFlagCache(httpContextAccessor);

            Func<CancellationToken, Task<IReadOnlyDictionary<string, FeatureFlag>>> fetcher = _ =>
                Task.FromResult((IReadOnlyDictionary<string, FeatureFlag>)new Dictionary<string, FeatureFlag>());

            // Act
            var result = await cache.GetAndCacheFeatureFlagsAsync(distinctId, fetcher, CancellationToken.None);

            // Assert
            Assert.Equal(cachedFeatureFlags, result);
        }

        [Fact]
        public async Task DoesNotCacheIfHttpContextIsNull()
        {
            // Arrange
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns((HttpContext)null!);

            var cache = new HttpContextFeatureFlagCache(httpContextAccessor);
            var distinctId = "user123";
            var featureFlags = new Dictionary<string, FeatureFlag>
            {
                { "feature1", new FeatureFlag(Key: "feature1", IsEnabled: true, null, null) }
            };

            Func<CancellationToken, Task<IReadOnlyDictionary<string, FeatureFlag>>> fetcher = _ =>
                Task.FromResult((IReadOnlyDictionary<string, FeatureFlag>)featureFlags);

            // Act
            var result = await cache.GetAndCacheFeatureFlagsAsync(distinctId, fetcher, CancellationToken.None);

            // Assert
            Assert.Equal(featureFlags, result);
        }
    }
}