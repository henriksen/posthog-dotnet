using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostHog.Config;
using PostHog.Library;
using PostHog.Versioning;

namespace PostHog.Api;

/// <summary>
/// PostHog API client for capturing events and managing user tracking
/// </summary>
internal sealed class PostHogApiClient : IDisposable
{
    const string LibraryName = "posthog-dotnet";

    readonly TimeProvider _timeProvider;
    readonly HttpClient _httpClient;
    readonly IOptions<PostHogOptions> _options;

    /// <summary>
    /// Initialize a new PostHog client
    /// </summary>
    /// <param name="options">The options used to configure this client.</param>
    /// <param name="timeProvider"></param>
    /// <param name="logger">The logger.</param>
    public PostHogApiClient(IOptions<PostHogOptions> options, TimeProvider timeProvider, ILogger logger)
    {
        _options = options;

        _timeProvider = timeProvider;
        _httpClient = new HttpClient();

        logger.LogTraceApiClientCreated(HostUrl);
    }

    Uri HostUrl => _options.Value.HostUrl;

    string ProjectApiKey => _options.Value.ProjectApiKey
                            ?? throw new InvalidOperationException("The Project API Key is not configured.");

    /// <summary>
    /// Capture an event with optional properties
    /// </summary>
    public async Task<ApiResult> CaptureBatchAsync(
        IEnumerable<CapturedEvent> events,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(HostUrl, "batch");

        var payload = new Dictionary<string, object>
        {
            ["historical_migrations"] = false,
            ["batch"] = events.ToReadOnlyList()
        };

        PrepareAndMutatePayload(payload);

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload, cancellationToken)
               ?? new ApiResult(0);
    }

    /// <summary>
    /// Method to send an event to the PostHog API's /capture endpoint. This is used for
    /// capturing events, identify, alias, etc.
    /// </summary>
    public async Task<ApiResult> SendEventAsync(
        Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        PrepareAndMutatePayload(payload);

        var endpointUrl = new Uri(HostUrl, "capture");

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload, cancellationToken)
               ?? new ApiResult(0);
    }

    public async Task<FeatureFlagsApiResult> RequestFeatureFlagsAsync(
        string distinctUserId,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(HostUrl, "decide?v=3");

        var payload = new Dictionary<string, object>
        {
            ["distinct_id"] = distinctUserId,
        };

        PrepareAndMutatePayload(payload);

        return await _httpClient.PostJsonAsync<FeatureFlagsApiResult>(endpointUrl, payload, cancellationToken)
               ?? new FeatureFlagsApiResult();
    }

    void PrepareAndMutatePayload(Dictionary<string, object> payload)
    {
        if (payload.GetValueOrDefault("properties") is Dictionary<string, object> properties)
        {
            properties["$lib"] = LibraryName;
            properties["$lib_version"] = VersionConstants.Version;
        }

        payload["api_key"] = ProjectApiKey;
        payload["timestamp"] = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
    }

    /// <summary>
    /// Dispose of HttpClient
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

internal static partial class PostHogApiClientLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Api Client Created: {HostUrl}")]
    public static partial void LogTraceApiClientCreated(this ILogger logger, Uri hostUrl);
}