using System.Text.Json;
using System.Text.Json.Serialization;
using PostHog.Json;

namespace PostHog.Api;

public record LocalEvaluationApiResult(
    IReadOnlyList<LocalFeatureFlag> Flags,
    [property: JsonPropertyName("group_type_mapping")]
    IReadOnlyDictionary<string, string> GroupTypeMapping,
    IReadOnlyDictionary<string, ConditionContainer> Cohorts);

public record LocalFeatureFlag(
    int Id,
    [property: JsonPropertyName("team_id")]
    int TeamId,
    string Name,
    string Key,
    FeatureFlagFilters Filters,
    bool Deleted,
    bool Active,
    [property: JsonPropertyName("ensure_experience_continuity")]
    bool EnsureExperienceContinuity);


public record FeatureFlagFilters(
    IReadOnlyList<FeatureFlagGroup> Groups,
    IReadOnlyDictionary<string, string> Payloads,
    Multivariate? Multivariate);

public record FeatureFlagGroup(
    string? Variant,
    IReadOnlyList<FilterProperty> Properties,
    [property: JsonPropertyName("rollout_percentage")]
    int RolloutPercentage);

public record Multivariate(IReadOnlyCollection<Variant> Variants);

public record Variant(
    string Key,
    string Name,
    [property: JsonPropertyName("rollout_percentage")]
    int RolloutPercentage);

public record FilterProperty(
    string Key,
    string Type,
    JsonElement Value,
    string Operator,
    [property: JsonPropertyName("group_type_index")]
    int? GroupTypeIndex = null)
{
    public virtual bool Equals(FilterProperty? other)
    {
        if (other is null)
        {
            return false;
        }

        return Key == other.Key
               && Type == other.Type
               && JsonComparison.AreJsonElementsEqual(Value, other.Value)
               && Operator == other.Operator
               && GroupTypeIndex == other.GroupTypeIndex;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Key);
        hash.Add(Type);
        hash.Add(Value.GetRawText());
        hash.Add(Operator);
        hash.Add(GroupTypeIndex);
        return hash.ToHashCode();
    }
}

public record ConditionGroup(string Type, IReadOnlyList<FilterProperty> Values)
{
    public virtual bool Equals(ConditionGroup? other)
    {
        if (other is null)
        {
            return false;
        }

        return Type == other.Type
               && Values.SequenceEqual(other.Values);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type);
        foreach (var value in Values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}

public record ConditionContainer(string Type, IReadOnlyList<ConditionGroup> Values)
{
    public virtual bool Equals(ConditionContainer? other)
    {
        if (other is null)
        {
            return false;
        }

        return Type == other.Type
               && Values.SequenceEqual(other.Values);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type);
        foreach (var value in Values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}