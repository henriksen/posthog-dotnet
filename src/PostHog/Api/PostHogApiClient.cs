using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PostHog.Library;
using PostHog.Versioning;

namespace PostHog.Api;

/// <summary>
/// PostHog API client for capturing events and managing user tracking
/// </summary>
internal sealed class PostHogApiClient : IDisposable
{
    public const string LibraryName = "posthog-dotnet";

    readonly string _projectApiKey;
    readonly Uri _hostUrl;
    readonly HttpClient _httpClient;

    /// <summary>
    /// Initialize a new PostHog client
    /// </summary>
    /// <param name="apiKey">Your PostHog project API key</param>
    /// <param name="hostUrl">Optional custom host URL (defaults to PostHog cloud)</param>
    /// <param name="logger">The logger.</param>
    public PostHogApiClient(string apiKey, Uri hostUrl, ILogger logger)
    {
        _projectApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _hostUrl = hostUrl ?? throw new ArgumentException("HostUrl cannot be null", nameof(hostUrl));
        var hostUrlString = hostUrl.ToString();
        _hostUrl = new Uri(hostUrlString.TrimEnd());
        _httpClient = new HttpClient();

        logger.LogTraceApiClientCreated(_hostUrl);
    }

    /// <summary>
    /// Capture an event with optional properties
    /// </summary>
    public async Task<ApiResult> CaptureBatchAsync(
        IEnumerable<CapturedEvent> events,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(_hostUrl, "batch");

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

        var endpointUrl = new Uri(_hostUrl, "capture");

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload, cancellationToken)
               ?? new ApiResult(0);
    }

    public async Task<FeatureFlagsApiResult> RequestFeatureFlagsAsync(
        string distinctUserId,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(_hostUrl, "decide?v=3");

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

        payload["api_key"] = _projectApiKey;
        payload["timestamp"] = DateTime.UtcNow;
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