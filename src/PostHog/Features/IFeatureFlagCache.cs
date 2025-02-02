using PostHog.Features;

namespace PostHog;

/// <summary>
/// Used to cache feature flags for a duration appropriate to the environment.
/// </summary>
public interface IFeatureFlagCache
{
    /// <summary>
    /// Attempts to retrieve the feature flags from the cache. If the feature flags are not in the cache, then
    /// they are fetched and stored in the cache.
    /// </summary>
    /// <param name="distinctId">The distinct id. Used as a cache key.</param>
    /// <param name="fetcher">The feature flag fetcher.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    /// <returns>The set of feature flags.</returns>
    Task<IReadOnlyDictionary<string, FeatureFlag>> GetAndCacheFeatureFlagsAsync(
        string distinctId,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, FeatureFlag>>> fetcher,
        CancellationToken cancellationToken);
}

/// <summary>
/// A null cache that does not cache feature flags. It always calls the fetcher.
/// </summary>
public sealed class NullFeatureFlagCache : IFeatureFlagCache
{
    public static readonly NullFeatureFlagCache Instance = new();

    private NullFeatureFlagCache()
    {
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, FeatureFlag>> GetAndCacheFeatureFlagsAsync(
        string distinctId,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, FeatureFlag>>> fetcher,
        CancellationToken cancellationToken)
    {
        fetcher = fetcher ?? throw new ArgumentNullException(nameof(fetcher));

        return await fetcher(cancellationToken);
    }
}

