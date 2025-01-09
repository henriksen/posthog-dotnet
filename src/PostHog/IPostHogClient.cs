using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Api;

namespace PostHog;

/// <summary>
/// Interface for the PostHog client. This is the main interface for interacting with PostHog.
/// Use this to identify users and capture events.
/// </summary>
public interface IPostHogClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <remarks>
    /// When you call Identify for a user, PostHog creates a
    /// <see href="https://posthog.com/docs/data/persons">Person Profile</see> of that user. You can use these person
    /// properties to better capture, analyze, and utilize user data. Whenever possible, we recommend passing in all
    /// person properties you have available each time you call identify, as this ensures their person profile on
    /// PostHog is up to date.
    /// </remarks>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="userProperties">Information about the user you want to be able to filter or group by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<ApiResult> IdentifyAsync(
        string distinctId,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken);

    /// <summary>
    /// Captures an event.
    /// </summary>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="properties">The properties to send along with the event.</param>
    void Capture(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties);

    /// <summary>
    /// Retrieves all the feature flags.
    /// </summary>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    Task<IReadOnlyDictionary<string, FeatureFlag>> GetFeatureFlagsAsync(
        string distinctId,
        CancellationToken cancellationToken);

    Task FlushAsync();
}