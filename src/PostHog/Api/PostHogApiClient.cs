using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PostHog.Json;
using PostHog.Library;

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
    public async Task<ApiResult> CaptureAsync(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties,
        CancellationToken cancellationToken)
    {
        properties ??= new Dictionary<string, object>();
        properties["$lib"] = LibraryName;

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _projectApiKey,
            ["event"] = eventName,
            ["distinct_id"] = distinctId,
            ["timestamp"] = DateTime.UtcNow,
            ["properties"] = properties
        };

        return await SendEventAsync(payload, cancellationToken);
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
            ["api_key"] = _projectApiKey,
            ["historical_migrations"] = false,
            ["batch"] = events.ToReadOnlyList(),
            ["$lib"] = LibraryName
        };

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload, cancellationToken)
               ?? new ApiResult(0);
    }

    /// <summary>
    /// Identify a user with additional properties
    /// </summary>
    public async Task<ApiResult> IdentifyPersonAsync(
        string distinctId,
        Dictionary<string, object> properties,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _projectApiKey,
            ["event"] = "$identify",
            ["distinct_id"] = distinctId,
            ["$set"] = properties
        };

        return await SendEventAsync(payload, cancellationToken);
    }

    /// <summary>
    /// Identify a group with additional properties
    /// </summary>
    public async Task<ApiResult> IdentifyGroupAsync(
        string type,
        StringOrValue<int> key,
        Dictionary<string, object>? properties,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _projectApiKey,
            ["event"] = "$groupidentify",
            ["distinct_id"] = $"{type}_{key}",
            ["properties"] = new Dictionary<string, object>
            {
                ["$group_type"] = type,
                ["$group_key"] = key,
                ["$group_set"] = properties ?? new()
            }
        };

        return await SendEventAsync(payload, cancellationToken);
    }

    /// <summary>
    /// Unlink future events with the current user. Call this when a user logs out.
    /// </summary>
    /// <param name="distinctId">The current user id.</param>
    /// <param name="cancellationToken"></param>
    public async Task ResetAsync(string distinctId, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _projectApiKey,
            ["event"] = "$reset",
            ["distinct_id"] = distinctId
        };

        await SendEventAsync(payload, cancellationToken);
    }

    /// <summary>
    /// Internal method to send events to PostHog
    /// </summary>
    async Task<ApiResult> SendEventAsync(Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(_hostUrl, "capture");

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload, cancellationToken)
               ?? new ApiResult(0);
    }

    public async Task<FeatureFlagsApiResult> RequestFeatureFlagsAsync(
        string distinctUserId,
        CancellationToken cancellationToken)
    {
        var endpointUrl = new Uri(_hostUrl, "decide?v=3");

        var requestBody = new Dictionary<string, string>
        {
            ["api_key"] = _projectApiKey,
            ["distinct_id"] = distinctUserId,
            ["$lib"] = "posthog-dotnet"
        };

        return await _httpClient.PostJsonAsync<FeatureFlagsApiResult>(endpointUrl, requestBody, cancellationToken)
               ?? new FeatureFlagsApiResult();
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