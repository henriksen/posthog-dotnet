using System.Collections;

namespace PostHog;

/// <summary>
/// When calling an API that requires a group or set of groups, such as evaluating feature flags,
/// use this to specify the groups. This also provides a way to specify additional group properties.
/// </summary>
public class GroupProperties : IEnumerable<Group>
{
    readonly Dictionary<string, Group> _groups = new();

    /// <summary>
    /// Attempts to add the specified groupType and groupKey to this collection.
    /// </summary>
    /// <param name="groupType">The type of group in PostHog. For example, company, project, etc.</param>
    /// <param name="groupKey">The identifier for the group such as the ID of the group in the database.</param>
    /// <returns><c>true</c> if the group was added. <c>false</c> if the group type already exists.</returns>
    public bool TryAdd(string groupType, string groupKey) => _groups.TryAdd(groupType, new Group(groupType, groupKey));

    /// <summary>
    /// Adds a <see cref="Group"/> with the specified groupType and groupKey to the groups.
    /// </summary>
    /// <param name="groupType">The type of group in PostHog. For example, company, project, etc.</param>
    /// <param name="groupKey">The identifier for the group such as the ID of the group in the database.</param>
    /// <exception cref="ArgumentNullException">Thrown if a group with this group type already exists.</exception>
    public void Add(string groupType, string groupKey)
    {
        if (TryAdd(groupType, groupKey))
        {
            return;
        }
        ThrowArgumentExceptionIfGroupWithGroupTypeExists(groupType);;
    }

    /// <summary>
    /// Attempts to add the specified group to this collection.
    /// </summary>
    /// <param name="group">The group to add.</param>
    /// <returns><c>true</c> if the group was added. <c>false</c> if the group type already exists.</returns>
    public bool TryAdd(Group group) => _groups.TryAdd((group ?? throw new ArgumentNullException(nameof(group))).GroupKey, group);

    /// <summary>
    /// Adds a <see cref="Group"/> to this collection.
    /// </summary>
    /// <param name="group">The group to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if a group with this group type already exists.</exception>
    public void Add(Group group)
    {
        group = group ?? throw new ArgumentNullException(nameof(group));

        if (_groups.TryAdd(group.GroupType, group))
        {
            return;
        }
        ThrowArgumentExceptionIfGroupWithGroupTypeExists(group.GroupKey);
    }

    static void ThrowArgumentExceptionIfGroupWithGroupTypeExists(string groupType) =>
        throw new ArgumentException($"A group with the `group_type` of '{groupType}' already exists.", nameof(groupType));

    /// <summary>
    /// Adds the groups and group properties to the specified properties dictionary.
    /// </summary>
    /// <param name="properties">A properties dictionary used to pass group properties to the API.</param>
    internal void AddToProperties(Dictionary<string, object> properties)
    {
        properties = properties ?? throw new ArgumentNullException(nameof(properties));

        if (_groups.Count is 0)
        {
            return;
        }

        var groups = properties.TryGetValue("groups", out var groupsValue)
            ? (Dictionary<string, string>)groupsValue
            : new Dictionary<string, string>();

        var groupProperties = properties.TryGetValue("group_properties", out var groupPropertiesValue)
            ? (Dictionary<string, Dictionary<string, object>>)groupPropertiesValue
            : new Dictionary<string, Dictionary<string, object>>();

        foreach (var group in _groups.Values)
        {
            group.AddToGroupProperties(ref groups, ref groupProperties);
        }

        if (groups is { Count: > 0})
        {
            properties["groups"] = groups;
        }

        if (groupProperties is { Count: > 0 })
        {
            properties["group_properties"] = groupProperties;
        }
    }

    public IEnumerator<Group> GetEnumerator() => _groups.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// The indexer for this <see cref="GroupProperties"/> used to get or set a group.
    /// </summary>
    /// <param name="groupType"></param>
    public Group this[string groupType]
    {
        get => _groups[groupType];
        set => _groups[groupType] = value;
    }
}

/// <summary>
/// Represents a group and its properties.
/// </summary>
/// <param name="GroupType">The type of group in PostHog. For example, company, project, etc.</param>
/// <param name="GroupKey">The identifier for the group such as the ID of the group in the database.</param>
public record Group(string GroupType, string GroupKey)
{
    Dictionary<string, object>? _properties;

    /// <summary>
    /// Adds a property and its value to the group.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    /// <returns>The <see cref="Group"/> that contains these properties.</returns>
    public Group AddProperty(string name, object value)
    {
        _properties ??= new Dictionary<string, object>();
        _properties.Add(name, value);
        return this;
    }

    internal void AddToGroupProperties(
        ref Dictionary<string, string>? groups,
        ref Dictionary<string, Dictionary<string, object>>? groupProperties)
    {
        groups ??= new Dictionary<string, string>();
        groups[GroupType] = GroupKey;

        if (_properties is null or { Count: 0 })
        {
            return;
        }

        groupProperties ??= new Dictionary<string, Dictionary<string, object>>();

        groupProperties[GroupType] = new Dictionary<string, object>(_properties)
        {
            ["$group_key"] = GroupKey
        };
    }

    /// <summary>
    /// The indexer for <see cref="Group"/> used to get or set a property value.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <exception cref="KeyNotFoundException"></exception>
    public object this[string name]
    {
        get => _properties?[name] ?? throw new KeyNotFoundException();
        set
        {
            _properties ??= new Dictionary<string, object>();
            _properties[name] = value;
        }
    }
}

