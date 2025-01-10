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
}