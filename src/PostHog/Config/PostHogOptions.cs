using Microsoft.Extensions.Options;

namespace PostHog.Config;

/// <summary>
/// Options for configuring the PostHog client.
/// </summary>
public sealed class PostHogOptions : AsyncBatchHandlerOptions, IOptions<PostHogOptions>
{
    /// <summary>
    /// The project API key that identifies which project this client works with.
    /// </summary>
    /// <remarks>
    /// You can find this https://us.posthog.com/project/{YOUR_PROJECT_ID}/settings/project
    /// </remarks>
    public string? ProjectApiKey { get; set; }

    /// <summary>
    /// Optional personal API key for local feature flag evaluation.
    /// </summary>
    /// <remarks>
    ///
    /// You can find this https://us.posthog.com/project/{YOUR_PROJECT_ID}/settings/user-api-keys
    /// When developing an ASP.NET Core project locally, we recommend setting this in your user secrets.
    /// <c>
    /// dotnet user-secrets --project your/project/path.csproj set PostHog:PersonalApiKey YOUR_PERSONAL_API_KEY
    /// </c>
    /// In other cases, use an appropriate secrets manager, configuration provider, or environment variable.
    /// </remarks>
    public string? PersonalApiKey { get; set; }

    /// <summary>
    /// PostHog API host, usually 'https://us.i.posthog.com' (default) or 'https://eu.i.posthog.com'
    /// </summary>
    public Uri HostUrl { get; set; } = new("https://us.i.posthog.com");

    // Explicit implementation to hide this value from most users.
    // This is here to make it easier to instantiate the client with the options.
    PostHogOptions IOptions<PostHogOptions>.Value => this;
}