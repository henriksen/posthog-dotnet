using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostHog.Config;
using PostHog.Library;
using PostHog.Versioning;

namespace PostHog.Api;

/// <inheritdoc cref="IPostHogApiClient" />
public sealed class PostHogApiClient : IPostHogApiClient
{
    internal const string LibraryName = "posthog-dotnet";

    readonly TimeProvider _timeProvider;
    readonly HttpClient _httpClient;
    readonly IOptions<PostHogOptions> _options;

    /// <summary>
    /// Initialize a new PostHog client
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> used to make requests.</param>
    /// <param name="options">The options used to configure this client.</param>
    /// <param name="timeProvider"></param>
    /// <param name="logger">The logger.</param>
    public PostHogApiClient(
        HttpClient httpClient,
        IOptions<PostHogOptions> options,
        TimeProvider timeProvider,
        ILogger<PostHogApiClient> logger)
    {
        _options = options;

        _timeProvider = timeProvider;

        _httpClient = httpClient;

        logger.LogTraceApiClientCreated(HostUrl);
    }

    /// <summary>
    /// Initialize a new PostHog client
    /// </summary>
    /// <param name="options">The options used to configure this client.</param>
    /// <param name="timeProvider"></param>
    /// <param name="logger">The logger.</param>
    public PostHogApiClient(
        IOptions<PostHogOptions> options,
        TimeProvider timeProvider,
        ILogger<PostHogApiClient> logger)
        : this(
#pragma warning disable CA2000
            // LoggingHttpMessageHandler is disposed when we dispose the HttpClient
            new HttpClient(new LoggingHttpMessageHandler(logger)
            {
                InnerHandler = new HttpClientHandler()
            }), options, timeProvider, logger)
#pragma warning restore CA2000
    {
    }

    Uri HostUrl => _options.Value.HostUrl;

    string ProjectApiKey => _options.Value.ProjectApiKey
                            ?? throw new InvalidOperationException("The Project API Key is not configured.");

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<ApiResult> SendEventAsync(
        Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        payload = payload ?? throw new ArgumentNullException(nameof(payload));

        PrepareAndMutatePayload(payload);

        var endpointUrl = new Uri(HostUrl, "capture");

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload, cancellationToken)
               ?? new ApiResult(0);
    }

    /// <inheritdoc/>
    public async Task<FeatureFlagsApiResult> GetFeatureFlagsAsync(
        string distinctUserId,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(HostUrl, "decide?v=3");

        var payload = new Dictionary<string, object>
        {
            ["distinct_id"] = distinctUserId,
        };

        if (personProperties is { Count: > 0 })
        {
            payload["person_properties"] = personProperties;
        }

        groupProperties?.AddToPayload(payload);

        PrepareAndMutatePayload(payload);

        return await _httpClient.PostJsonAsync<FeatureFlagsApiResult>(endpointUrl, payload, cancellationToken)
               ?? new FeatureFlagsApiResult();
    }

    /// <inheritdoc/>
    public Version Version => new(VersionConstants.Version);

    void PrepareAndMutatePayload(Dictionary<string, object> payload)
    {
        if (payload.GetValueOrDefault("properties") is Dictionary<string, object> properties)
        {
            properties["$lib"] = LibraryName;
            properties["$lib_version"] = VersionConstants.Version;
        }

        payload["api_key"] = ProjectApiKey;
        payload["timestamp"] = _timeProvider.GetUtcNow(); // ISO 8601
    }

    /// <summary>
    /// Dispose of HttpClient
    /// </summary>
    public void Dispose() => _httpClient.Dispose();
}

internal static partial class PostHogApiClientLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Api Client Created: {HostUrl}")]
    public static partial void LogTraceApiClientCreated(this ILogger<PostHogApiClient> logger, Uri hostUrl);
}