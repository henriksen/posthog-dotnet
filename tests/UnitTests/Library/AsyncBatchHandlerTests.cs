using Microsoft.Extensions.Time.Testing;
using PostHog.Config;
using PostHog.Library;

public class AsyncBatchHandlerTests
{
    [Fact]
    public async Task CallsBatchHandlerWhenThresholdMet()
    {
        var options = new AsyncBatchHandlerOptions
        {
            FlushAt = 3,
            FlushInterval = TimeSpan.FromHours(3)
        };
        var items = new List<int>();
        Func<IEnumerable<int>, Task> handlerFunc = batch =>
        {
            items.AddRange(batch);
            return Task.CompletedTask;
        };
        await using var batchHandler = new AsyncBatchHandler<int>(handlerFunc, options);

        batchHandler.Enqueue(1);
        Assert.Empty(items);
        batchHandler.Enqueue(2);
        Assert.Empty(items);
        batchHandler.Enqueue(3);
        Assert.Equal([1, 2, 3], items);
    }

    [Fact]
    public async Task FlushesBatchWhenDisposed()
    {
        var options = new AsyncBatchHandlerOptions
        {
            FlushAt = 3,
            FlushInterval = TimeSpan.FromHours(3)
        };
        var items = new List<int>();
        Func<IEnumerable<int>, Task> handlerFunc = batch =>
        {
            items.AddRange(batch);
            return Task.CompletedTask;
        };

        await using (var batchHandler = new AsyncBatchHandler<int>(handlerFunc, options))
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
        var options = new AsyncBatchHandlerOptions
        {
            FlushAt = 10,
            FlushInterval = TimeSpan.FromSeconds(2)
        };
        var items = new List<int>();
        Func<IEnumerable<int>, Task> handlerFunc = batch =>
        {
            items.AddRange(batch);
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
        Assert.Empty(items);
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal([1, 2, 3], items);
    }
}