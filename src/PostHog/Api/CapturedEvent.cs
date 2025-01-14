using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PostHog.Api;

/// <summary>
/// A captured event that will be sent as part of a batch.
/// </summary>
/// <param name="eventName">The name of the event</param>
public class CapturedEvent(
    string eventName,
    string distinctId,
    Dictionary<string, object> properties,
    DateTimeOffset timestamp)
{
    [JsonPropertyName("event")]
    public string EventName => eventName;

    [JsonPropertyName("distinct_id")]
    public string DistinctId => distinctId;

    /// <summary>
    /// The properties to send with the event.
    /// </summary>
    public Dictionary<string, object> Properties => properties;

    /// <summary>
    /// The timestamp of the event.
    /// </summary>
    public DateTimeOffset Timestamp => timestamp;
}