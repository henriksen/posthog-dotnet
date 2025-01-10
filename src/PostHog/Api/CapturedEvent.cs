using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PostHog.Api;

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