using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PostHog.Config;

namespace PostHog.Library;

/// <summary>
/// Processes enqueued items in batches on a timer or when the queue count reaches a certain threshold.
/// </summary>
/// <typeparam name="TItem">The type of item being batched up.</typeparam>
internal sealed class AsyncBatchHandler<TItem> : IDisposable, IAsyncDisposable
{
    readonly ConcurrentQueue<TItem> _concurrentQueue = new();
    readonly IOptions<AsyncBatchHandlerOptions> _options;
    readonly Func<IEnumerable<TItem>, Task> _batchHandlerFunc;
    readonly ILogger _logger;
    readonly PeriodicTimer _timer;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly Task _pollingTask;
    readonly SemaphoreSlim _flushSemaphore = new(initialCount: 1, maxCount: 1);

    public AsyncBatchHandler(
        Func<IEnumerable<TItem>, Task> batchHandlerFunc,
        IOptions<AsyncBatchHandlerOptions> options,
        TimeProvider timeProvider,
        ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _batchHandlerFunc = batchHandlerFunc;
        _logger = logger;
        _timer = new PeriodicTimer(options.Value.FlushInterval, timeProvider);
        _pollingTask = PollAsync(_cancellationTokenSource.Token);
    }

    public AsyncBatchHandler(
        Func<IEnumerable<TItem>, Task> batchHandlerFunc,
        TimeProvider timeProvider,
        IOptions<AsyncBatchHandlerOptions> options)
        : this(batchHandlerFunc, options, timeProvider, NullLogger.Instance)
    {
    }

    public void Enqueue(TItem item)
    {
        _concurrentQueue.Enqueue(item);

        if (_concurrentQueue.Count >= _options.Value.FlushAt)
        {
            Task.Run(async () =>
            {
                _logger.LogTraceFlushCalledOnCaptureFlushAt(_options.Value.FlushAt, _concurrentQueue.Count);
                await FlushImplementationAsync();
            });
        }
    }

    public int Count => _concurrentQueue.Count;

    async Task PollAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
            {
                _logger.LogTraceFlushCalledOnFlushInterval(_options.Value.FlushInterval, _concurrentQueue.Count);
                await FlushImplementationAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task FlushImplementationAsync()
    {
        // We want to make sure only one flush is happening at a time.
        await _flushSemaphore.WaitAsync();
        try
        {
            while (_concurrentQueue.TryDequeueBatch(_options.Value.MaxBatchSize, out var batch))
            {
                _logger.LogDebugSendingBatch(batch.Count);
                await _batchHandlerFunc(batch);
                //await _apiClient.CaptureBatchAsync(batch, CancellationToken.None);
            }
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    public async Task FlushAsync()
    {
        _logger.LogInfoFlushCalledDirectly(_concurrentQueue.Count);
        await FlushImplementationAsync();
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
        _logger.LogTraceFlushCalledInDispose(_concurrentQueue.Count);
        await FlushAsync();

        _flushSemaphore.Dispose();
    }
}

internal static partial class AsyncFlushingQueueLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Sending Batch: {Count} items")]
    public static partial void LogDebugSendingBatch(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Flush called on capture because FlushAt ({FlushAt}) count met, {Count} items in the queue")]
    public static partial void LogTraceFlushCalledOnCaptureFlushAt(
        this ILogger logger, int flushAt, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Trace,
        Message = "Flush called on the Flush Interval: {Interval}, {Count} items in the queue")]
    public static partial void LogTraceFlushCalledOnFlushInterval(
        this ILogger logger,
        TimeSpan interval,
        int count);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Trace,
        Message = "Flush called because we're disposing:{Count} items in the queue")]
    public static partial void LogTraceFlushCalledInDispose(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Flush called directly via code: {Count} items in the queue")]
    public static partial void LogInfoFlushCalledDirectly(this ILogger logger, int count);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "DisposeAsync called")]
    public static partial void LogInfoDisposeAsyncCalled(this ILogger logger);
}
