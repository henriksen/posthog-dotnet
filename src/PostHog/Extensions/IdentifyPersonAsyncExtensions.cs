using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Api;

namespace PostHog; // Intentionally put in the root namespace.

public static class IdentifyPersonAsyncExtensions
{
    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        CancellationToken cancellationToken) =>
        (client ?? throw new ArgumentNullException(nameof(client)))
            .IdentifyPersonAsync(
            distinctId,
            userPropertiesToSet: new(),
            userPropertiesToSetOnce: new(),
            cancellationToken);

    public static Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        CancellationToken cancellationToken)
        => client.IdentifyPersonAsync(
            distinctId,
            email,
            name,
            additionalUserPropertiesToSet: new(),
            userPropertiesToSetOnce: new(),
            cancellationToken);

    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="email">The email to set for the user.</param>
    /// <param name="name">The name to set for the user</param>
    /// <param name="additionalUserPropertiesToSet">
    /// Key value pairs to store as properties of the user in addition to the already specified "email" and "name".
    /// Any key value pairs in this dictionary that match existing property keys will overwrite those properties.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        Dictionary<string, object> additionalUserPropertiesToSet,
        CancellationToken cancellationToken)
        => await client.IdentifyPersonAsync(
            distinctId,
            email,
            name,
            additionalUserPropertiesToSet,
            userPropertiesToSetOnce: new(),
            cancellationToken);

    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="email">The email to set for the user.</param>
    /// <param name="name">The name to set for the user</param>
    /// <param name="additionalUserPropertiesToSet">
    /// Key value pairs to store as properties of the user in addition to the already specified "email" and "name".
    /// Any key value pairs in this dictionary that match existing property keys will overwrite those properties.
    /// </param>
    /// <param name="userPropertiesToSetOnce">User properties to set only once (ex: Sign up date). If a property already exists, then the
    /// value in this dictionary is ignored.
    /// </param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        Dictionary<string, object> additionalUserPropertiesToSet,
        Dictionary<string, object> userPropertiesToSetOnce,
        CancellationToken cancellationToken)
    {;
        client = client ?? throw new ArgumentNullException(nameof(client));
        additionalUserPropertiesToSet = additionalUserPropertiesToSet ?? throw new ArgumentNullException(nameof(additionalUserPropertiesToSet));
        userPropertiesToSetOnce = userPropertiesToSetOnce ?? throw new ArgumentNullException(nameof(userPropertiesToSetOnce));

        if (email is not null)
        {
            additionalUserPropertiesToSet["email"] = email;
        }

        if (name is not null)
        {
            additionalUserPropertiesToSet["name"] = name;
        }

        return await client.IdentifyPersonAsync(distinctId,
                additionalUserPropertiesToSet,
                userPropertiesToSetOnce,
                cancellationToken);
    }

    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    public static Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId) =>
        (client ?? throw new ArgumentNullException(nameof(client)))
        .IdentifyPersonAsync(
            distinctId,
            userPropertiesToSet: new(),
            userPropertiesToSetOnce: new(),
            CancellationToken.None);
}