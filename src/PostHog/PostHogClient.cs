using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PostHog.Api;
using PostHog.Config;
using PostHog.Features;
using PostHog.Json;
using PostHog.Library;
using PostHog.Versioning;

namespace PostHog;

/// <inheritdoc cref="IPostHogClient" />
public sealed class PostHogClient : IPostHogClient
{
    readonly AsyncBatchHandler<CapturedEvent> _asyncBatchHandler;
    readonly IPostHogApiClient _apiClient;
    readonly IFeatureFlagCache _featureFlagCache;
    private readonly TimeProvider _timeProvider;
    readonly ILogger<PostHogClient> _logger;

    /// <summary>
    /// Constructs a <see cref="PostHogClient"/> with the specified <paramref name="options"/>,
    /// <see cref="TimeProvider"/>, and <paramref name="logger"/>.
    /// </summary>
    /// <param name="postHogApiClient">The <see cref="IPostHogApiClient"/> used to make requests.</param>
    /// <param name="featureFlagCache">Caches feature flags for a duration appropriate to the environment.</param>
    /// <param name="options">The options used to configure the client.</param>
    /// <param name="timeProvider">The time provider <see cref="TimeProvider"/> to use to determine time.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public PostHogClient(
        IPostHogApiClient postHogApiClient,
        IFeatureFlagCache featureFlagCache,
        IOptions<PostHogOptions> options,
        TimeProvider timeProvider,
        ILogger<PostHogClient> logger)
    {
        options = options ?? throw new ArgumentNullException(nameof(options));
        _apiClient = postHogApiClient;
        _featureFlagCache = featureFlagCache;
        _asyncBatchHandler = new(
            batch => _apiClient.CaptureBatchAsync(batch, CancellationToken.None),
            options,
            timeProvider,
            logger);

        _featureFlagCache = featureFlagCache;
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
        TimeProvider.System,
        NullLogger<PostHogClient>.Instance)
    {
    }

    /// <inheritdoc/>
    public async Task<ApiResult> IdentifyPersonAsync(
        string distinctId,
        Dictionary<string, object> userPropertiesToSet,
        Dictionary<string, object> userPropertiesToSetOnce,
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
        Dictionary<string, object> properties,
        CancellationToken cancellationToken)
    => _apiClient.IdentifyGroupAsync(type, key, properties, cancellationToken);

    /// <inheritdoc/>
    public void CaptureEvent(
        string distinctId,
        string eventName,
        Dictionary<string, object> properties,
        Dictionary<string, object> groups)
    {
        properties = properties ?? throw new ArgumentNullException(nameof(properties));
        groups = groups ?? throw new ArgumentNullException(nameof(groups));

        if (groups.Count > 0)
        {
            properties["$groups"] = groups;
        }

        var capturedEvent = new CapturedEvent(
            eventName,
            distinctId,
            properties,
            _timeProvider.GetUtcNow().DateTime);

        _asyncBatchHandler.Enqueue(capturedEvent);

        _logger.LogTraceCaptureCalled(eventName, properties.Count, _asyncBatchHandler.Count);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, FeatureFlag>> GetFeatureFlagsAsync(
        string distinctId,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties,
        CancellationToken cancellationToken)
        => await _featureFlagCache.GetAndCacheFeatureFlagsAsync(
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
            kvp => new FeatureFlag(
                kvp.Key,
                kvp.Value.Value,
                kvp.Value.StringValue,
                results.FeatureFlagPayloads.GetValueOrDefault(kvp.Key)));
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