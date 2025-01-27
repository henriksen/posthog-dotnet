namespace PostHog.Api;

/// <summary>
/// Extensions to add information to the API payload before making a request.
/// </summary>
internal static class PayloadExtensions
{
    /// <summary>
    /// Adds the groups and group properties to the specified properties dictionary.
    /// </summary>
    /// <param name="groupCollection">The source <see cref="GroupCollection"/>.</param>
    /// <param name="payload">A dictionary that is the payload to the PostHog API.</param>
    internal static void AddToPayload(this GroupCollection groupCollection, Dictionary<string, object> payload)
    {
        payload = payload ?? throw new ArgumentNullException(nameof(payload));

        if (groupCollection.Count is 0)
        {
            return;
        }

        var groups = payload.TryGetValue("groups", out var groupsValue)
            ? (Dictionary<string, string>)groupsValue
            : new Dictionary<string, string>();

        var groupProperties = payload.TryGetValue("group_properties", out var groupPropertiesValue)
            ? (Dictionary<string, Dictionary<string, object?>>)groupPropertiesValue
            : new Dictionary<string, Dictionary<string, object?>>();

        foreach (var group in groupCollection)
        {
            group.AddToPayload(ref groups, ref groupProperties);
        }

        if (groups is { Count: > 0 })
        {
            payload["groups"] = groups;
        }

        if (groupProperties is { Count: > 0 })
        {
            payload["group_properties"] = groupProperties;
        }
    }

    static void AddToPayload(
        this Group featureFlagGroup,
        ref Dictionary<string, string>? groups,
        ref Dictionary<string, Dictionary<string, object?>>? groupProperties)
    {
        groups ??= new Dictionary<string, string>();
        groups[featureFlagGroup.GroupType] = featureFlagGroup.GroupKey;

        if (featureFlagGroup.Properties is null or { Count: 0 })
        {
            return;
        }

        groupProperties ??= new Dictionary<string, Dictionary<string, object?>>();

        groupProperties[featureFlagGroup.GroupType] = new Dictionary<string, object?>(featureFlagGroup.Properties)
        {
            ["$group_key"] = featureFlagGroup.GroupKey
        };
    }

}