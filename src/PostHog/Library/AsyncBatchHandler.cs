using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PostHog.Config;

namespace PostHog.Library;
internal sealed class AsyncBatchHandler<TItem> : IDisposable, IAsyncDisposable
{
    readonly Channel<TItem> _channel;
    readonly IOptions<AsyncBatchHandlerOptions> _options;
    readonly Func<IEnumerable<TItem>, Task> _batchHandlerFunc;
    readonly ILogger _logger;
    readonly PeriodicTimer _timer;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly SemaphoreSlim _flushSignal = new(0); // Used to signal when a flush is needed
    volatile int _disposing;
    volatile int _flushing;

    public AsyncBatchHandler(
        Func<IEnumerable<TItem>, Task> batchHandlerFunc,
        IOptions<AsyncBatchHandlerOptions> options,
        TimeProvider timeProvider,
        ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _batchHandlerFunc = batchHandlerFunc;
        _logger = logger;
        _channel = Channel.CreateBounded<TItem>(new BoundedChannelOptions(_options.Value.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _timer = new PeriodicTimer(options.Value.FlushInterval, timeProvider);
        _ = ProcessQueueAsync(_cancellationTokenSource.Token);
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
        if (!_channel.Writer.TryWrite(item))
        {
            _logger.LogWarningDroppingItem(_options.Value.MaxQueueSize);
        }

        if (_channel.Reader.Count >= _options.Value.FlushAt)
        {
            // Signal that a flush is needed.
            _flushSignal.Release();
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var flushTask = _flushSignal.WaitAsync(cancellationToken);
                var timerTask = _timer.WaitForNextTickAsync(cancellationToken).AsTask();

                var completedTask = await Task.WhenAny(flushTask, timerTask);
                if (completedTask == flushTask || completedTask == timerTask)
                {
                    await FlushBatchAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    async Task FlushBatchAsync()
    {
        // If we're flushing, don't start another flush.
        if (Interlocked.CompareExchange(ref _flushing, 1, 0) == 1)
        {
            return;
        }

        var batch = new List<TItem>();

        while (_channel.Reader.TryRead(out var item))
        {
            batch.Add(item);
            if (batch.Count >= _options.Value.MaxBatchSize)
            {
                break;
            }
        }

        if (batch.Count > 0)
        {
            _logger.LogDebugSendingBatch(batch.Count);
            await _batchHandlerFunc(batch);
        }

        Interlocked.Exchange(ref _flushing, 0);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposing, 1) == 1)
        {
            _logger.LogWarningDisposeCalledTwice();
            return;
        }

        _logger.LogInfoDisposeAsyncCalled();
        await _cancellationTokenSource.CancelAsync();
        _timer.Dispose();
        await FlushBatchAsync();
        _cancellationTokenSource.Dispose();
        _flushSignal.Dispose();
        _channel.Writer.Complete();
    }

    public int Count => _channel.Reader.Count;

    public async Task FlushAsync()
    {
        await FlushBatchAsync();
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

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Dropping oldest item because queue size exceeded: {MaxQueueSize}")]
    public static partial void LogWarningDroppingItem(this ILogger logger, int maxQueueSize);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Dispose called a second time. Ignoring")]
    public static partial void LogWarningDisposeCalledTwice(this ILogger logger);
}
