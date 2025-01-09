using System;

namespace PostHog;

/// <summary>
/// Options for configuring the PostHog client.
/// </summary>
public class PostHogOptions
{
    /// <summary>
    /// The project API key.
    /// </summary>
    public string? ProjectApiKey { get; set; }

    /// <summary>
    /// PostHog API host, usually 'https://us.i.posthog.com' (default) or 'https://eu.i.posthog.com'
    /// </summary>
    public Uri HostUrl { get; set; } = new("https://us.i.posthog.com");

    /// <summary>
    /// The maximum number of cached messages either in memory or on the local storage. (Default: 1000)
    /// </summary>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// The number of events to queue before sending to PostHog (Default: 20)
    /// </summary>
    public int FlushAt { get; set; } = 20;

    /// <summary>
    /// The interval in milliseconds between periodic flushes. (Default: 30s)
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
}