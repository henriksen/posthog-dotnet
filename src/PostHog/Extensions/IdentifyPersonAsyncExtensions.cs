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
            personPropertiesToSet: null,
            personPropertiesToSetOnce: null,
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
            personPropertiesToSet: null,
            personPropertiesToSetOnce: null,
            cancellationToken);

    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="email">The email to set for the user.</param>
    /// <param name="name">The name to set for the user</param>
    /// <param name="personPropertiesToSet">
    /// Key value pairs to store as properties of the user in addition to the already specified "email" and "name".
    /// Any key value pairs in this dictionary that match existing property keys will overwrite those properties.
    /// </param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    public static async Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        Dictionary<string, object>? personPropertiesToSet,
        CancellationToken cancellationToken)
        => await client.IdentifyPersonAsync(
            distinctId,
            email,
            name,
            personPropertiesToSet,
            personPropertiesToSetOnce: null,
            cancellationToken);

    /// <summary>
    /// Identifies a user with the specified distinct ID and user properties.
    /// See <seealso href="https://posthog.com/docs/getting-started/identify-users"/>.
    /// </summary>
    /// <param name="client">The <see cref="IPostHogClient"/>.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="email">The email to set for the user.</param>
    /// <param name="name">The name to set for the user</param>
    /// <param name="personPropertiesToSet">
    /// Key value pairs to store as properties of the user in addition to the already specified "email" and "name".
    /// Any key value pairs in this dictionary that match existing property keys will overwrite those properties.
    /// </param>
    /// <param name="personPropertiesToSetOnce">User properties to set only once (ex: Sign up date). If a property already exists, then the
    /// value in this dictionary is ignored.
    /// </param>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    public static async Task<ApiResult> IdentifyPersonAsync(
        this IPostHogClient client,
        string distinctId,
        string? email,
        string? name,
        Dictionary<string, object>? personPropertiesToSet,
        Dictionary<string, object>? personPropertiesToSetOnce,
        CancellationToken cancellationToken)
    {
        if (email is not null)
        {
            personPropertiesToSet ??= new Dictionary<string, object>();
            personPropertiesToSet["email"] = email;
        }

        if (name is not null)
        {
            personPropertiesToSet ??= new Dictionary<string, object>();
            personPropertiesToSet["name"] = name;
        }

        return await NotNull(client).IdentifyPersonAsync(distinctId,
                personPropertiesToSet,
                personPropertiesToSetOnce,
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
            personPropertiesToSet: null,
            personPropertiesToSetOnce: null,
            CancellationToken.None);
}