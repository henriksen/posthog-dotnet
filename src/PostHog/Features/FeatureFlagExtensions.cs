using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Features;

namespace PostHog; // Intentionally put in the root namespace.

public static class FeatureFlagExtensions
{
    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="userProperties"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static async Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        var result = await client.GetFeatureFlagAsync(distinctId, featureKey, userProperties, cancellationToken);

        return result?.IsEnabled;
    }

    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature does not
    /// exist.
    /// </returns>
    public static Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        CancellationToken cancellationToken)
        => client.IsFeatureEnabledAsync(
            distinctId,
            featureKey,
            new Dictionary<string, object>(),
            cancellationToken);

    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey)
        => client.IsFeatureEnabledAsync(
            distinctId,
            featureKey,
            new Dictionary<string, object>(),
            CancellationToken.None);

    /// <summary>
    /// Determines whether a feature is enabled for the specified user.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="userProperties"></param>
    /// <returns>
    /// <c>true</c> if the feature is enabled for the user. <c>false</c> if not. <c>null</c> if the feature is undefined.
    /// </returns>
    public static Task<bool?> IsFeatureEnabledAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        Dictionary<string, object> userProperties)
        => client.IsFeatureEnabledAsync(distinctId, featureKey, userProperties, CancellationToken.None);

    /// <summary>
    /// Retrieves a feature flag.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="featureKey">The name of the feature flag.</param>
    /// <param name="userProperties"></param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The feature flag or null if it does not exist or is not enabled.</returns>
    public static async Task<FeatureFlag?> GetFeatureFlagAsync(
        this IPostHogClient client,
        string distinctId,
        string featureKey,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken)
    {
        client = client ?? throw new ArgumentNullException(nameof(client));
        var flags = await client.GetFeatureFlagsAsync(distinctId, cancellationToken);
        return flags.GetValueOrDefault(featureKey);
    }
}