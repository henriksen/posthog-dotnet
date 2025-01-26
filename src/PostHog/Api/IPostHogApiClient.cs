using PostHog.Config;

namespace PostHog.Api;

/// <summary>
/// PostHog API client for capturing events and managing user tracking
/// </summary>
public interface IPostHogApiClient : IDisposable
{
    /// <summary>
    /// Capture an event with optional properties
    /// </summary>
    /// <param name="events">The events to send to PostHog.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
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
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally, if personalApiKey is present. Not needed if using remote evaluation, but can be used to override remote values for the purposes of feature flag evaluation.</param>
    /// <param name="groupProperties">Optional: What group properties are known. Used to compute flags locally, if personalApiKey is present.  Not needed if using remote evaluation, but can be used to override remote values for the purposes of feature flag evaluation.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="DecideApiResult"/>.</returns>
    Task<DecideApiResult?> GetFeatureFlagsFromDecideAsync(
        string distinctUserId,
        Dictionary<string, object>? personProperties,
        GroupCollection? groupProperties,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all the feature flags for the project by making a request to the
    /// <c>/api/feature_flag/local_evaluation</c> endpoint. This requires that a Personal API Key is set in
    /// <see cref="PostHogOptions"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="LocalEvaluationApiResult"/> containing all the feature flags.</returns>
    Task<LocalEvaluationApiResult?> GetFeatureFlagsForLocalEvaluationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The version of the client.
    /// </summary>
    Version Version { get; }
}