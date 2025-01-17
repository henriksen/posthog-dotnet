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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="FeatureFlagsApiResult"/>.</returns>
    Task<FeatureFlagsApiResult> GetFeatureFlagsAsync(
        string distinctUserId,
        CancellationToken cancellationToken);
}