using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PostHog.Api;
using PostHog.Library;
using PostHog.Models;

namespace PostHog;

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

    public PostHogClient(PostHogOptions options, ILogger<PostHogClient> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        var projectApiKey = options.ProjectApiKey
                            ?? throw new InvalidOperationException("Project API key is required.");
        _apiClient = new PostHogApiClient(projectApiKey, options.HostUrl);
        _timer = new PeriodicTimer(options.FlushInterval);
        _pollingTask = PollAsync(_cancellationTokenSource.Token);

        _logger = logger;

        _logger.LogInfoClientCreated(options.MaxBatchSize);
    }

    public PostHogClient(PostHogOptions options) : this(options, NullLogger<PostHogClient>.Instance)
    {
    }

    public PostHogClient(string projectApiKey) : this(new PostHogOptions { ProjectApiKey = projectApiKey })
    {
    }

    public async Task<ApiResult> IdentifyAsync(
        string distinctId,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken)
        => await _apiClient.IdentifyAsync(distinctId, userProperties, cancellationToken);

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

        if (_concurrentQueue.Count >= _options.FlushAt)
        {
            Flush();
        }
    }

    public async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                await FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {

        }
    }

    public Task ResetAsync(string distinctId, CancellationToken cancellationToken)
        => _apiClient.ResetAsync(distinctId, cancellationToken);

    void Flush()
    {
        Task.Run(async () => await FlushAsync()).Start();
    }

    public async Task FlushAsync()
    {
        // We want to make sure only one flush is happening at a time.
        await _flushSemaphore.WaitAsync();
        try
        {
            _logger.LogInfoFlushCalled(_concurrentQueue.Count);
            while (_concurrentQueue.TryDequeueBatch(_options.MaxBatchSize, out var batch))
            {
                _logger.LogDebugBatchSent(batch.Count);
                await _apiClient.CaptureBatchAsync(batch, CancellationToken.None);
            }
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

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
        Message = "BatchSent: {Count} items")]
    public static partial void LogDebugBatchSent(this ILogger<PostHogClient> logger, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Flush called: {Count} items")]
    public static partial void LogInfoFlushCalled(this ILogger<PostHogClient> logger, int count);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "PostHog Client created with Max Batch Size: {MaxBatchSize}")]
    public static partial void LogInfoClientCreated(this ILogger<PostHogClient> logger, int maxBatchSize);
}