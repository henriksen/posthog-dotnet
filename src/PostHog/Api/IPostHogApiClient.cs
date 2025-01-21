namespace PostHog.Api;

/// <summary>
/// PostHog API client for capturing events and managing user tracking
/// </summary>
public interface IPostHogApiClient : IDisposable
{
    /// <summary>
    /// Capture an event with optional properties
    /// </summary>
    Task<ApiResult> CaptureBatchAsync(
        IEnumerable<CapturedEvent> events,
        CancellationToken cancellationToken);

    /// <summary>
    /// Method to send an event to the PostHog API's /capture endpoint. This is used for
    /// capturing events, identify, alias, etc.
    /// </summary>
    Task<ApiResult> SendEventAsync(
        Dictionary<string, object> payload,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all the feature flags for the user by making a request to the <c>/decide</c> endpoint.
    /// </summary>
    /// <param name="distinctUserId">The Id of the user.</param>
    /// <param name="groups">Optional: What groups are currently active. This is a mapping of group type to group key. Required if the flag depends on groups. This contains the group type and key to match on.</param>
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally, if personalApiKey is present. Not needed if using remote evaluation, but can be used to override remote values for the purposes of feature flag evaluation.</param>
    /// <param name="groupProperties">Optional: What group properties are known. Used to compute flags locally, if personalApiKey is present.  Not needed if using remote evaluation, but can be used to override remote values for the purposes of feature flag evaluation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="FeatureFlagsApiResult"/>.</returns>
    Task<FeatureFlagsApiResult> GetFeatureFlagsAsync(
        string distinctUserId,
        Dictionary<string, object>? groups,
        Dictionary<string, object>? personProperties,
        Dictionary<string, object>? groupProperties,
        CancellationToken cancellationToken);
}