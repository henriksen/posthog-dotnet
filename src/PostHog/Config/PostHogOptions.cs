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

    /// <summary>
    /// The size limit of the $feature_flag_sent cache.
    /// </summary>
    /// <remarks>
    /// When evaluating a feature flag and <c>sendFeatureFlagEvents</c> is <c>true</c>, the client captures a
    /// $feature_flag_called event. To limit the cost to the customer, it only sends this event once per
    /// feature flag/distinct_id combination. To do this, it caches the sent events. This property sets the
    /// the size limit of that cache.
    /// </remarks>
    public long FeatureFlagSentCacheSizeLimit { get; set; } = 50_000;

    /// <summary>
    /// Sets a sliding expiration for the $feature_flag_sent cache. See <see cref="FeatureFlagSentCacheSizeLimit"/>
    /// for more about the cache.
    /// </summary>
    public TimeSpan FeatureFlagSentCacheSlidingExpiration { get; set; } = TimeSpan.FromMinutes(10);

    // Explicit implementation to hide this value from most users.
    // This is here to make it easier to instantiate the client with the options.
    PostHogOptions IOptions<PostHogOptions>.Value => this;
}