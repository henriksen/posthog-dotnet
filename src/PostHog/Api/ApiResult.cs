using PostHog.Json;

namespace PostHog.Api;

/// <summary>
/// Result of a PostHog API call.
/// </summary>
public class ApiResult(StringOrValue<int> status)
{
    /// <summary>
    /// The status.
    /// </summary>
    /// <remarks>
    /// For Capture, this returns {"status": 1} if the event was captured successfully.
    /// For Batch, this returns {"status": "Ok"} if all events were captured successfully.
    /// </remarks>
    public StringOrValue<int> Status => status;
}