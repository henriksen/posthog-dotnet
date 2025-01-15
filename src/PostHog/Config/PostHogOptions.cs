namespace PostHog.Config;

/// <summary>
/// Options for configuring the PostHog client.
/// </summary>
public class PostHogOptions : AsyncBatchHandlerOptions
{
    /// <summary>
    /// The project API key.
    /// </summary>
    public string? ProjectApiKey { get; set; }

    /// <summary>
    /// PostHog API host, usually 'https://us.i.posthog.com' (default) or 'https://eu.i.posthog.com'
    /// </summary>
    public Uri HostUrl { get; set; } = new("https://us.i.posthog.com");
}