using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PostHog.Versioning;

namespace PostHog.Api;

/// <summary>
/// A captured event that will be sent as part of a batch.
/// </summary>
public class CapturedEvent
{
    public CapturedEvent(
        string eventName,
        string distinctId,
        Dictionary<string, object> properties,
        DateTimeOffset timestamp)
    {
        EventName = eventName;
        DistinctId = distinctId;
        Timestamp = timestamp;

        Properties = properties;

        // Every event has to have these properties.
        Properties["$lib"] = PostHogApiClient.LibraryName;
        Properties["$lib_version"] = VersionConstants.Version;
    }

    /// <summary>
    /// The event name.
    /// </summary>
    [JsonPropertyName("event")]
    public string EventName { get; }

    /// <summary>
    /// The distinct identifier.
    /// </summary>
    [JsonPropertyName("distinct_id")]
    public string DistinctId { get; private set; }

    /// <summary>
    /// The properties to send with the event.
    /// </summary>
    public Dictionary<string, object> Properties { get; }

    /// <summary>
    /// The timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}