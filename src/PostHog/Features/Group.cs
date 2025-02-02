using PostHog.Library;

namespace PostHog;

using static Ensure;

/// <summary>
/// Represents a group and its properties.
/// </summary>
/// <param name="GroupType">The type of group in PostHog. For example, company, project, etc.</param>
/// <param name="GroupKey">The identifier for the group such as the ID of the group in the database.</param>
/// <param name="Properties">The group properties to associate with this group. These can be used in feature flag calls to override whats on the server.</param>
public record Group(string GroupType, string GroupKey, Dictionary<string, object?> Properties)
{
    /// <summary>
    /// Constructs a <see cref="Group"/>
    /// </summary>
    /// <param name="groupType">The type of group in PostHog. For example, company, project, etc.</param>
    /// <param name="groupKey">The identifier for the group such as the ID of the group in the database.</param>
    public Group(string groupType, string groupKey)
        : this(groupType, groupKey, Properties: [])
    {
    }

    /// <summary>
    /// Adds a property and its value to the group.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    /// <returns>The <see cref="Group"/> that contains these properties.</returns>
    public Group AddProperty(string name, object value)
    {
        NotNull(Properties).Add(name, value);
        return this;
    }

    /// <summary>
    /// The indexer for <see cref="Group"/> used to get or set a property value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    public object this[string name]
    {
        get => NotNull(Properties)[name] ?? throw new KeyNotFoundException();
        set => NotNull(Properties)[name] = value;
    }
}