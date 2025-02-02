using PostHog.Api;
using static PostHog.Library.Ensure;

namespace PostHog; // Intentionally put in the root namespace.

public static class IdentifyPersonAsyncExtensions
{
    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    public static Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        CancellationToken cancellationToken)
        => NotNull(client)
            .IdentifyPersonAsync(
            distinctId,
            userPropertiesToSet: null,
            userPropertiesToSetOnce: null,
            cancellationToken);

    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="email">An email to associate with the person.</param>
    /// <param name="name">The person's name.</param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
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
            additionalUserPropertiesToSet: null,
            userPropertiesToSetOnce: null,
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
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    public static async Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        Dictionary<string, object>? additionalUserPropertiesToSet,
        CancellationToken cancellationToken)
        => await client.IdentifyPersonAsync(
            distinctId,
            email,
            name,
            additionalUserPropertiesToSet,
            userPropertiesToSetOnce: null,
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
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    public static async Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        Dictionary<string, object>? additionalUserPropertiesToSet,
        Dictionary<string, object>? userPropertiesToSetOnce,
        CancellationToken cancellationToken)
    {
        if (email is not null)
        {
            additionalUserPropertiesToSet ??= new Dictionary<string, object>();
            additionalUserPropertiesToSet["email"] = email;
        }

        if (name is not null)
        {
            additionalUserPropertiesToSet ??= new Dictionary<string, object>();
            additionalUserPropertiesToSet["name"] = name;
        }

        return await NotNull(client).IdentifyPersonAsync(distinctId,
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
        string distinctId)
        => NotNull(client).IdentifyPersonAsync(
            distinctId,
            userPropertiesToSet: null,
            userPropertiesToSetOnce: null,
            CancellationToken.None);
}