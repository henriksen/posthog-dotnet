using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Library;
using PostHog.Models;

namespace PostHog;

/// <summary>
/// PostHog client for capturing events and managing user tracking
/// </summary>
public sealed class PostHogClient : IDisposable
{
    const string LibraryName = "posthog-dotnet";

    readonly string _apiKey;
    readonly Uri _hostUrl;
    readonly HttpClient _httpClient;

    /// <summary>
    /// Initialize a new PostHog client
    /// </summary>
    /// <param name="apiKey">Your PostHog project API key</param>
    /// <param name="hostUrl">Optional custom host URL (defaults to PostHog cloud)</param>
    public PostHogClient(string apiKey, Uri hostUrl)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _hostUrl = hostUrl ?? throw new ArgumentException("HostUrl cannot be null", nameof(hostUrl));
        var hostUrlString = hostUrl.ToString();
        _hostUrl = new Uri(hostUrlString.TrimEnd());
        _httpClient = new HttpClient();
    }

    public PostHogClient(string apiKey) : this(apiKey, new Uri("https://app.posthog.com/")) { }

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
            ["api_key"] = _apiKey,
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
        var endpointUrl = $"{_hostUrl}/batch/";

        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _apiKey,
            ["historical_migrations"] = false,
            ["batch"] = events.ToReadOnlyList(),
            ["$lib"] = LibraryName
        };

        return await _httpClient.PostJsonAsync<ApiResult>(new Uri(endpointUrl), payload, cancellationToken)
               ?? new ApiResult(0);
    }

    /// <summary>
    /// Identify a user with a distinct ID.
    /// </summary>
    /// <remarks>
    /// This is a public endpoint and does not require authentication, but it does require
    /// a valid API key in the body of the request.
    /// </remarks>
    /// <param name="distinctId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ApiResult> IdentifyAsync(string distinctId, CancellationToken cancellationToken) =>
        IdentifyAsync(distinctId, new Dictionary<string, object>(), cancellationToken);

    /// <summary>
    /// Identify a user with additional properties
    /// </summary>
    public async Task<ApiResult> IdentifyAsync(
        string distinctId,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["api_key"] = _apiKey,
            ["event"] = "$identify",
            ["distinct_id"] = distinctId,
            ["$set"] = userProperties
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
            ["api_key"] = _apiKey,
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
        var endpointUrl = $"{_hostUrl}/capture/";

        return await _httpClient.PostJsonAsync<ApiResult>(new Uri(endpointUrl), payload, cancellationToken) ?? new ApiResult(0);
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
/// A captured event that will be sent as part of a batch.
/// </summary>
/// <param name="eventName">The name of the event</param>
public class CapturedEvent(string eventName, string distinctId)
{
    [JsonPropertyName("event")]
    public string EventName => eventName;

    [JsonPropertyName("distinct_id")]
    public string DistinctId => distinctId;

    public Dictionary<string, object> Properties { get; } = new();

    public DateTime Timestamp { get; } = DateTime.UtcNow;

    public CapturedEvent WithProperties(Dictionary<string, object> properties)
    {
        properties ??= new Dictionary<string, object>();
        foreach (var (key, value) in properties)
        {
            Properties[key] = value;
        }

        return this;
    }
}
