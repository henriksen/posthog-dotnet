using System;
using System.Collections.Concurrent;
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
    readonly ConcurrentQueue<CapturedEvent> _concurrentQueue = new();
    readonly PostHogApiClient _apiClient;
    readonly PeriodicTimer _timer;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly Task _pollingTask;
    readonly PostHogOptions _options;
    readonly ILogger<PostHogClient> _logger;
    readonly SemaphoreSlim _flushSemaphore = new(1, 1);

    /// <summary>
    /// Constructs a <see cref="PostHogClient"/> with the specified <paramref name="options"/> and
    /// <paramref name="logger"/>.
    /// </summary>
    /// <param name="options">The options used to configure the client.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    public PostHogClient(PostHogOptions options, ILogger<PostHogClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        var projectApiKey = options.ProjectApiKey
                            ?? throw new InvalidOperationException("Project API key is required.");

        _apiClient = new PostHogApiClient(projectApiKey, options.HostUrl, logger);
        _timer = new PeriodicTimer(options.FlushInterval);
        _pollingTask = PollAsync(_cancellationTokenSource.Token);

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

        _concurrentQueue.Enqueue(capturedEvent);

        _logger.LogTraceCaptureCalled(eventName, properties.Count, _concurrentQueue.Count);

        if (_concurrentQueue.Count >= _options.FlushAt)
        {
            Task.Run(async () =>
            {
                _logger.LogTraceFlushCalledOnCaptureFlushAt(_options.FlushAt, _concurrentQueue.Count);
                await FlushImplementationAsync();
            });
        }
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

    async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                _logger.LogTraceFlushCalledOnFlushInterval(_options.FlushInterval, _concurrentQueue.Count);
                await FlushImplementationAsync();
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    /// <inheritdoc/>
    public async Task FlushAsync()
    {
        _logger.LogInfoFlushCalledDirectly(_concurrentQueue.Count);
        await FlushImplementationAsync();
    }

    async Task FlushImplementationAsync()
    {
        // We want to make sure only one flush is happening at a time.
        await _flushSemaphore.WaitAsync();
        try
        {
            while (_concurrentQueue.TryDequeueBatch(_options.MaxBatchSize, out var batch))
            {
                _logger.LogDebugSendingBatch(batch.Count);
                await _apiClient.CaptureBatchAsync(batch, CancellationToken.None);
            }
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

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
        await _cancellationTokenSource.CancelAsync();
        await _pollingTask;
        _cancellationTokenSource.Dispose();
        _timer.Dispose();

        // Flush whatever we got.
        await FlushAsync();

        _apiClient.Dispose();
        _flushSemaphore.Dispose();
    }
}

internal static partial class SkillRunnerLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "DisposeAsync called")]
    public static partial void LogInfoDisposeAsyncCalled(this ILogger<PostHogClient> logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Sending Batch: {Count} items")]
    public static partial void LogDebugSendingBatch(this ILogger<PostHogClient> logger, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Trace,
        Message = "Flush called on capture because FlushAt ({FlushAt}) count met, {Count} items in the queue")]
    public static partial void LogTraceFlushCalledOnCaptureFlushAt(
        this ILogger<PostHogClient> logger, int flushAt, int count);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Trace,
        Message = "Flush called on the Flush Interval: {Interval}, {Count} items in the queue")]
    public static partial void LogTraceFlushCalledOnFlushInterval(
        this ILogger<PostHogClient> logger,
        TimeSpan interval,
        int count);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Flush called directly via code: {Count} items in the queue")]
    public static partial void LogInfoFlushCalledDirectly(this ILogger<PostHogClient> logger, int count);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "PostHog Client created with Max Batch Size: {MaxBatchSize}, Flush Interval: {FlushInterval}, and FlushAt: {FlushAt}")]
    public static partial void LogInfoClientCreated(
        this ILogger<PostHogClient> logger,
        int maxBatchSize,
        TimeSpan flushInterval,
        int flushAt);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Capture called for event {EventName} with {PropertiesCount} properties. {Count} items in the queue")]
    public static partial void LogTraceCaptureCalled(
        this ILogger<PostHogClient> logger,
        string eventName,
        int propertiesCount,
        int count);
}