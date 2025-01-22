namespace PostHog;

/// <summary>
/// Represents a group and its properties.
/// </summary>
/// <param name="GroupType">The type of group in PostHog. For example, company, project, etc.</param>
/// <param name="GroupKey">The identifier for the group such as the ID of the group in the database.</param>
public record Group(string GroupType, string GroupKey)
{
    /// <summary>
    /// Constructs a <see cref="Group"/>
    /// </summary>
    /// <param name="groupType">The type of group in PostHog. For example, company, project, etc.</param>
    /// <param name="groupKey">The identifier for the group such as the ID of the group in the database.</param>
    /// <param name="properties">The group properties to associate with this group. These can be used in feature flag calls to override whats on the server.</param>
    public Group(string groupType, string groupKey, Dictionary<string, object>? properties)
        : this(groupType, groupKey)
    {
        Properties = properties;
    }

    public Dictionary<string, object>? Properties { get; private set; }

    /// <summary>
    /// Adds a property and its value to the group.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    /// <returns>The <see cref="Group"/> that contains these properties.</returns>
    public Group AddProperty(string name, object value)
    {
        Properties ??= new Dictionary<string, object>();
        Properties.Add(name, value);
        return this;
    }

    /// <summary>
    /// The indexer for <see cref="Group"/> used to get or set a property value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    public object this[string name]
    {
        get
        {
            Properties ??= new Dictionary<string, object>();
            return Properties[name] ?? throw new KeyNotFoundException();
        }
        set
        {
            Properties ??= new Dictionary<string, object>();
            Properties[name] = value;
        }
    }
}