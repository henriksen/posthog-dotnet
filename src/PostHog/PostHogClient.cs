using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using PostHog.Library;
using PostHog.Models;

namespace PostHog;

/// <summary>
/// PostHog client for capturing events and managing user tracking
/// </summary>
public class PostHogClient : IDisposable
{
    readonly string _apiKey;
    readonly string _hostUrl;
    readonly HttpClient _httpClient;

    /// <summary>
    /// Initialize a new PostHog client
    /// </summary>
    /// <param name="apiKey">Your PostHog project API key</param>
    /// <param name="hostUrl">Optional custom host URL (defaults to PostHog cloud)</param>
    public PostHogClient(string apiKey, string hostUrl = "https://app.posthog.com/")
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _hostUrl = hostUrl.TrimEnd('/');
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Capture an event with optional properties
    /// </summary>
    public async Task<ApiResult> CaptureAsync(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties)
    {
        properties ??= new Dictionary<string, object>();
        properties["$lib"] = "posthog-dotnet";

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _apiKey,
            ["event"] = eventName,
            ["distinct_id"] = distinctId,
            ["timestamp"] = DateTime.UtcNow,
            ["properties"] = properties
        };

        return await SendEventAsync(payload);
    }

    /// <summary>
    /// Identify a user with a distinct ID.
    /// </summary>
    /// <remarks>
    /// This is a public endpoint and does not require authentication, but it does require
    /// a valid API key in the body of the request.
    /// </remarks>
    /// <param name="distinctId"></param>
    /// <returns></returns>
    public Task<ApiResult> IdentifyAsync(string distinctId) =>
        IdentifyAsync(distinctId, new Dictionary<string, object>());

    /// <summary>
    /// Identify a user with additional properties
    /// </summary>
    public async Task<ApiResult> IdentifyAsync(string distinctId, Dictionary<string, object> userProperties)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _apiKey,
            ["event"] = "$identify",
            ["distinct_id"] = distinctId,
            ["$set"] = userProperties
        };

        return await SendEventAsync(payload);
    }

    /// <summary>
    /// Unlink future events with the current user. Call this when a user logs out.
    /// </summary>
    /// <param name="distinctId">The current user id.</param>
    public async Task ResetAsync(string distinctId)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _apiKey,
            ["event"] = "$reset",
            ["distinct_id"] = distinctId
        };

        await SendEventAsync(payload);
    }

    /// <summary>
    /// Send batch of events
    /// </summary>
    public async Task BatchCaptureAsync(IEnumerable<EventData> events)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _apiKey,
            ["batch"] = events
        };

        await SendEventAsync(payload, isBatch: true);
    }

    /// <summary>
    /// Internal method to send events to PostHog
    /// </summary>
    private async Task<ApiResult> SendEventAsync(
        Dictionary<string, object> payload,
        bool isBatch = false)
    {
        var endpointUrl = isBatch
            ? $"{_hostUrl}/batch/"
            : $"{_hostUrl}/capture/";

        return await _httpClient.PostJsonAsync<ApiResult>(endpointUrl, payload) ?? new ApiResult(0);
    }

    /// <summary>
    /// Dispose of HttpClient
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Represents an event for batch capture
/// </summary>
public class EventData
{
    public string? Event { get; set; }
    public string? DistinctId { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}