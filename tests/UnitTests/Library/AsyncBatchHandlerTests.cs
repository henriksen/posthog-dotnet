using Microsoft.Extensions.Time.Testing;
using PostHog.Config;
using PostHog.Library;

public class AsyncBatchHandlerTests
{
    [Fact]
    public async Task CallsBatchHandlerWhenThresholdMet()
    {
        var options = new FakeOptions<AsyncBatchHandlerOptions>(new()
        {
            FlushAt = 3,
            FlushInterval = TimeSpan.FromHours(3)
        });
        var items = new List<int>();
        var handlerCompleteTask = new TaskCompletionSource();
        Func<IEnumerable<int>, Task> handlerFunc = batch =>
        {
            items.AddRange(batch);
            handlerCompleteTask.SetResult();
            return Task.CompletedTask;
        };
        await using var batchHandler = new AsyncBatchHandler<int>(handlerFunc, new FakeTimeProvider(), options);

        batchHandler.Enqueue(1);
        Assert.Empty(items);
        batchHandler.Enqueue(2);
        Assert.Empty(items);
        batchHandler.Enqueue(3);
        await handlerCompleteTask.Task;
        Assert.Equal([1, 2, 3], items);
    }

    [Fact]
    public async Task FlushesBatchWhenDisposed()
    {
        var options = new FakeOptions<AsyncBatchHandlerOptions>(new()
        {
            FlushAt = 3,
            FlushInterval = TimeSpan.FromHours(3)
        });
        var items = new List<int>();
        Func<IEnumerable<int>, Task> handlerFunc = batch =>
        {
            items.AddRange(batch);
            return Task.CompletedTask;
        };

        await using (var batchHandler = new AsyncBatchHandler<int>(handlerFunc, new FakeTimeProvider(), options))
        {
            batchHandler.Enqueue(1);
            Assert.Empty(items);
            batchHandler.Enqueue(2);
            Assert.Empty(items);
        }

        Assert.Equal([1, 2], items);
    }

    [Fact]
    public async Task FlushesBatchOnTimer()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new FakeOptions<AsyncBatchHandlerOptions>(new()
        {
            FlushAt = 10,
            FlushInterval = TimeSpan.FromSeconds(2)
        });
        var items = new List<int>();
        var handlerCompleteTask = new TaskCompletionSource();
        Func<IEnumerable<int>, Task> handlerFunc = batch =>
        {
            items.AddRange(batch);
            handlerCompleteTask.SetResult();
            return Task.CompletedTask;
        };

        await using var batchHandler = new AsyncBatchHandler<int>(handlerFunc, timeProvider, options);
        batchHandler.Enqueue(1);
        Assert.Empty(items);
        batchHandler.Enqueue(2);
        Assert.Empty(items);
        batchHandler.Enqueue(3);
        Assert.Empty(items);

        // Simulate the passage of time.
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        // Ensure empty because we only advanced 1 second, but the interval is 2 seconds.
        Assert.Empty(items);
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        // Ok, we should be flushing. Let's wait for that to complete.
        await handlerCompleteTask.Task;
        // The batch should be done flushing due to the timer interval.
        Assert.Equal([1, 2, 3], items);
    }
}