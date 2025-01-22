using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PostHog.Api;
using PostHog.Config;
using PostHog.Features;
using PostHog.Json;
using PostHog.Library;

namespace PostHog;

/// <inheritdoc cref="IPostHogClient" />
public sealed class PostHogClient : IPostHogClient
{
    readonly AsyncBatchHandler<CapturedEvent> _asyncBatchHandler;
    readonly IPostHogApiClient _apiClient;
    readonly IFeatureFlagCache _featureFlagsCache;
    readonly MemoryCache _featureFlagSentCache;
    readonly TimeProvider _timeProvider;
    readonly IOptions<PostHogOptions> _options;
    readonly ITaskScheduler _taskScheduler;
    readonly ILogger<PostHogClient> _logger;

    /// <summary>
    /// Constructs a <see cref="PostHogClient"/> with the specified <paramref name="options"/>,
    /// <see cref="TimeProvider"/>, and <paramref name="logger"/>.
    /// </summary>
    /// <param name="postHogApiClient">The <see cref="IPostHogApiClient"/> used to make requests.</param>
    /// <param name="featureFlagsCache">Caches feature flags for a duration appropriate to the environment.</param>
    /// <param name="options">The options used to configure the client.</param>
    /// <param name="taskScheduler">Used to run tasks on the background.</param>
    /// <param name="timeProvider">The time provider <see cref="TimeProvider"/> to use to determine time.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public PostHogClient(
        IPostHogApiClient postHogApiClient,
        IFeatureFlagCache featureFlagsCache,
        IOptions<PostHogOptions> options,
        ITaskScheduler taskScheduler,
        TimeProvider timeProvider,
        ILogger<PostHogClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
        _apiClient = postHogApiClient;
        _featureFlagsCache = featureFlagsCache;
        _asyncBatchHandler = new(
            batch => _apiClient.CaptureBatchAsync(batch, CancellationToken.None),
            options,
            taskScheduler,
            timeProvider,
            logger);

        _featureFlagsCache = featureFlagsCache;
        _featureFlagSentCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.FeatureFlagSentCacheSizeLimit,
            Clock = new TimeProviderSystemClock(timeProvider),
            CompactionPercentage = options.Value.FeatureFlagSentCacheCompactionPercentage,
        });

        _timeProvider = timeProvider;
        _logger = logger;

        _logger.LogInfoClientCreated(options.Value.MaxBatchSize, options.Value.FlushInterval, options.Value.FlushAt);
    }

    /// <summary>
    /// The simplest way to create a <see cref="PostHogClient"/>.
    /// </summary>
    /// <param name="options">The options used to configure the client.</param>
    public PostHogClient(IOptions<PostHogOptions> options) : this(
        new PostHogApiClient(options, TimeProvider.System, NullLogger<PostHogApiClient>.Instance),
        NullFeatureFlagCache.Instance,
        options,
        new TaskRunTaskScheduler(),
        TimeProvider.System,
        NullLogger<PostHogClient>.Instance)
    {
    }

    /// <inheritdoc/>
    public async Task<ApiResult> IdentifyPersonAsync(
        string distinctId,
        Dictionary<string, object>? userPropertiesToSet,
        Dictionary<string, object>? userPropertiesToSetOnce,
        CancellationToken cancellationToken)
        => await _apiClient.IdentifyPersonAsync(
            distinctId,
            userPropertiesToSet,
            userPropertiesToSetOnce,
            cancellationToken);

    /// <inheritdoc/>
    public Task<ApiResult> IdentifyGroupAsync(
        string type,
        StringOrValue<int> key,
        Dictionary<string, object>? properties,
        CancellationToken cancellationToken)
    => _apiClient.IdentifyGroupAsync(type, key, properties, cancellationToken);

    /// <inheritdoc/>
    public void CaptureEvent(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties,
        Dictionary<string, object>? groups)
    {

        if (groups is { Count: > 0 })
        {
            properties ??= new Dictionary<string, object>();
            properties["$groups"] = groups;
        }

        var capturedEvent = new CapturedEvent(
            eventName,
            distinctId,
            properties,
            timestamp: _timeProvider.GetUtcNow());

        _asyncBatchHandler.Enqueue(capturedEvent);

        _logger.LogTraceCaptureCalled(eventName, capturedEvent.Properties.Count, _asyncBatchHandler.Count);
    }

    /// <inheritdoc/>
    public async Task<bool?> IsFeatureEnabledAsync(
        string distinctId,
        string featureKey,
        FeatureFlagOptions? options,
        CancellationToken cancellationToken)
    {
        var result = await GetFeatureFlagAsync(
            distinctId,
            featureKey,
            options,
            cancellationToken);

        return result?.IsEnabled;
    }

    /// <inheritdoc/>
    public async Task<FeatureFlag?> GetFeatureFlagAsync(
        string distinctId,
        string featureKey,
        FeatureFlagOptions? options,
        CancellationToken cancellationToken)
    {
        var flags = await GetFeatureFlagsAsync(
            distinctId,
            options?.PersonProperties,
            options?.GroupProperties,
            cancellationToken);

        var flag = flags.GetValueOrDefault(featureKey);

        options ??= new FeatureFlagOptions(); // We need the defaults if options is null.

        var returnValue = options.SendFeatureFlagEvents
            ? _featureFlagSentCache.GetOrCreate((distinctId, featureKey),
                cacheEntry => CaptureFeatureFlagSentEvent(distinctId, featureKey, cacheEntry, flag))
            : flag;

        if (_featureFlagSentCache.Count >= _options.Value.FeatureFlagSentCacheSizeLimit)
        {
            // We need to fire and forget the compaction because it can be expensive.
            _taskScheduler.Run(
                () => _featureFlagSentCache.Compact(_options.Value.FeatureFlagSentCacheCompactionPercentage),
                cancellationToken);
        }

        return returnValue;
    }

    FeatureFlag? CaptureFeatureFlagSentEvent(
        string distinctId,
        string featureKey,
        ICacheEntry cacheEntry,
        FeatureFlag? flag)
    {
        cacheEntry.SetSize(1); // Each entry has a size of 1
        cacheEntry.SetPriority(CacheItemPriority.Low);
        cacheEntry.SetSlidingExpiration(_options.Value.FeatureFlagSentCacheSlidingExpiration);

        CaptureEvent(
            distinctId,
            eventName: "$feature_flag_called",
            properties: new Dictionary<string, object>
            {
                ["$feature_flag"] = featureKey,
                ["$feature_flag_response"] = flag.ToResponseObject(),
                ["locally_evaluated"] = false,
                [$"$feature/{featureKey}"] = flag.ToResponseObject()
            },
            // TODO: transform groupProperties into what we expect here.
            groups: new Dictionary<string, object>());

        return flag;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, FeatureFlag>> GetFeatureFlagsAsync(
        string distinctId,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties,
        CancellationToken cancellationToken)
        => await _featureFlagsCache.GetAndCacheFeatureFlagsAsync(
            distinctId,
            fetcher: ct => FetchFeatureFlagsAsync(
                distinctId,
                personProperties,
                groupProperties,
                ct), cancellationToken);

    async Task<IReadOnlyDictionary<string, FeatureFlag>> FetchFeatureFlagsAsync(
        string distinctId,
        Dictionary<string, object>? userProperties,
        GroupCollection? groupProperties,
        CancellationToken cancellationToken)
    {
        var results = await _apiClient.GetFeatureFlagsAsync(
            distinctId,
            userProperties,
            groupProperties,
            cancellationToken);
        return results.FeatureFlags.ToReadOnlyDictionary(
            kvp => kvp.Key,
            kvp => new FeatureFlag(kvp, results.FeatureFlagPayloads));
    }

    /// <inheritdoc/>
    public async Task FlushAsync() => await _asyncBatchHandler.FlushAsync();

    /// <inheritdoc/>
    public Version Version => _apiClient.Version;

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().Wait();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Stop the polling and wait for it.
        await _asyncBatchHandler.DisposeAsync();
        _apiClient.Dispose();
        _featureFlagSentCache.Dispose();
    }
}

internal static partial class PostHogClientLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "PostHog Client created with Max Batch Size: {MaxBatchSize}, Flush Interval: {FlushInterval}, and FlushAt: {FlushAt}")]
    public static partial void LogInfoClientCreated(
        this ILogger<PostHogClient> logger,
        int maxBatchSize,
        TimeSpan flushInterval,
        int flushAt);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Capture called for event {EventName} with {PropertiesCount} properties. {Count} items in the queue")]
    public static partial void LogTraceCaptureCalled(
        this ILogger<PostHogClient> logger,
        string eventName,
        int propertiesCount,
        int count);
}