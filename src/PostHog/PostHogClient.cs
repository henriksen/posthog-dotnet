using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PostHog.Api;
using PostHog.Config;
using PostHog.Json;
using PostHog.Library;
using PostHog.Models;

namespace PostHog;

/// <inheritdoc cref="IPostHogClient" />
public sealed class PostHogClient : IPostHogClient
{
    readonly AsyncBatchHandler<CapturedEvent> _asyncBatchHandler;
    readonly PostHogApiClient _apiClient;
    readonly ILogger<PostHogClient> _logger;

    /// <summary>
    /// Constructs a <see cref="PostHogClient"/> with the specified <paramref name="options"/> and
    /// <paramref name="logger"/>.
    /// </summary>
    /// <param name="options">The options used to configure the client.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public PostHogClient(PostHogOptions options, ILogger<PostHogClient> logger)
    {
        options = options ?? throw new ArgumentNullException(nameof(options));
        var projectApiKey = options.ProjectApiKey
                            ?? throw new InvalidOperationException("Project API key is required.");

        _apiClient = new PostHogApiClient(projectApiKey, options.HostUrl, logger);

        _asyncBatchHandler = new(
            batch => _apiClient.CaptureBatchAsync(batch, CancellationToken.None),
            options,
            TimeProvider.System,
            logger);

        _logger = logger;

        _logger.LogInfoClientCreated(options.MaxBatchSize, options.FlushInterval, options.FlushAt);
    }

    public PostHogClient(IOptions<PostHogOptions> options, ILogger<PostHogClient> logger)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value, logger)
    {
    }

    public PostHogClient(PostHogOptions options) : this(options, NullLogger<PostHogClient>.Instance)
    {
    }

    public PostHogClient(string projectApiKey) : this(new PostHogOptions { ProjectApiKey = projectApiKey })
    {
    }

    /// <inheritdoc/>
    public async Task<ApiResult> IdentifyPersonAsync(
        string distinctId,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken)
        => await _apiClient.IdentifyPersonAsync(distinctId, userProperties, cancellationToken);

    /// <inheritdoc/>
    public Task<ApiResult> IdentifyGroupAsync(
        string type,
        StringOrValue<int> key,
        Dictionary<string, object> properties,
        CancellationToken cancellationToken)
    => _apiClient.IdentifyGroupAsync(type, key, properties, cancellationToken);

    /// <inheritdoc/>
    public void Capture(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties)
    {
        properties ??= [];
        properties["$lib"] = PostHogApiClient.LibraryName;

        var capturedEvent = new CapturedEvent(eventName, distinctId)
            .WithProperties(properties);

        _asyncBatchHandler.Enqueue(capturedEvent);

        _logger.LogTraceCaptureCalled(eventName, properties.Count, _asyncBatchHandler.Count);
    }

    public async Task<IReadOnlyDictionary<string, FeatureFlag>> GetFeatureFlagsAsync(string distinctId, CancellationToken cancellationToken)
    {
        var results = await _apiClient.RequestFeatureFlagsAsync(distinctId, cancellationToken);
        return results.FeatureFlags.ToReadOnlyDictionary(
            kvp => kvp.Key,
            kvp => new FeatureFlag(
                kvp.Key,
                kvp.Value.Value,
                kvp.Value.StringValue,
                results.FeatureFlagPayloads.GetValueOrDefault(kvp.Key)));
    }

    public async Task<StringOrValue<bool>?> GetFeatureFlagAsync(
        string distinctId,
        string featureKey,
        Dictionary<string, object> userProperties)
    {
        var results = await _apiClient.RequestFeatureFlagsAsync(distinctId, CancellationToken.None);
        return results.FeatureFlags.GetValueOrDefault(featureKey);
    }

    /// <inheritdoc/>
    public async Task FlushAsync() => await _asyncBatchHandler.FlushAsync();

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _logger.LogInfoDisposeAsyncCalled();

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

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "DisposeAsync called")]
    public static partial void LogInfoDisposeAsyncCalled(this ILogger<PostHogClient> logger);
}