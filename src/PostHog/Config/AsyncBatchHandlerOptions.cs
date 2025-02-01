namespace PostHog.Config;

public class AsyncBatchHandlerOptions
{
    /// <summary>
    /// The maximum number of messages to send in a batch. (Default: 100)
    /// </summary>
    /// <remarks>
    /// This property ensures we don't try to send too much data in a single batch request.
    /// </remarks>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// The max number of messages to store in the queue before we start dropping messages. (Default: 1000)
    /// </summary>
    /// <remarks>
    /// This property prevents runaway growth of the queue in the case of network outage or a burst of messages.
    /// </remarks>
    public int MaxQueueSize { get; set; } = 1000;

    /// <summary>
    /// The number of events to queue before sending to PostHog (Default: 20)
    /// </summary>
    public int FlushAt { get; set; } = 20;

    /// <summary>
    /// The interval in milliseconds between periodic flushes. (Default: 30s)
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
}