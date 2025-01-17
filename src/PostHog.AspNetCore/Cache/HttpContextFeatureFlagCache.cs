using Microsoft.AspNetCore.Http;
using PostHog.Features;

namespace PostHog.Cache;

/// <summary>
/// An implementation of <see cref="IFeatureFlagCache"/> that uses the current <see cref="HttpContext"/> to cache
/// feature flags. If the <see cref="HttpContext"/> is not available, then the feature flags are not cached.
/// </summary>
public class HttpContextFeatureFlagCache(IHttpContextAccessor httpContextAccessor) : IFeatureFlagCache
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, FeatureFlag>> GetAndCacheFeatureFlagsAsync(
        string distinctId,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, FeatureFlag>>> fetcher,
        CancellationToken cancellationToken)
    {
        fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));

        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.Items[$"$feature_flags:{distinctId}"] is not IReadOnlyDictionary<string, FeatureFlag> flags)
        {
            flags = await fetcher(cancellationToken);
            if (httpContext is not null)
            {
                httpContext.Items[$"$feature_flags:{distinctId}"] = flags;
            }
        }

        return flags;
    }
}