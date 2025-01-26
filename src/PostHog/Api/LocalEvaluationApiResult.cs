using System.Collections.ObjectModel;
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
    FeatureFlagFilters? Filters,
    bool Deleted,
    bool Active,
    [property: JsonPropertyName("ensure_experience_continuity")]
    bool EnsureExperienceContinuity);

/// <summary>
/// Defines the targeting rules for a feature flag - essentially determining who sees what variant of the feature.
/// </summary>
/// <remarks>
/// In PostHog, this is stored as a JSON blob in the <c>posthog_featureflag</c> table.
/// </remarks>
/// <param name="Groups">These are sets of conditions that determine who sees the feature flag. If any group matches,
/// the flag is active for that user.</param>
/// <param name="Payloads"></param>
/// <param name="Multivariate"></param>
/// <param name="AggregationGroupTypeIndex"></param>
public record FeatureFlagFilters(
    IReadOnlyList<FeatureFlagGroup> Groups,
    IReadOnlyDictionary<string, string> Payloads,
    Multivariate? Multivariate = null,
    [property: JsonPropertyName("aggregation_group_type_index")]
    int? AggregationGroupTypeIndex = null)
{
    public FeatureFlagFilters() : this(Array.Empty<FeatureFlagGroup>(),
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()))
    {
    }
}

/// <summary>
/// Set of conditions that determine who sees the feature flag. If any group matches, the flag is active for that user.
/// </summary>
/// <param name="Variant">Optional override to serve a specific variant to users matching this group.</param>
/// <param name="Properties">Conditions about the user/group. (e.g. "user is in country X" or "user is in cohort Y")</param>
/// <param name="RolloutPercentage">Optional percentage (0-100) for gradual rollouts. Defaults to 100.</param>
public record FeatureFlagGroup(
    IReadOnlyList<FilterProperty> Properties,
    string? Variant = null,
    [property: JsonPropertyName("rollout_percentage")]
    int RolloutPercentage = 100);

public record Multivariate(IReadOnlyCollection<Variant> Variants);

public record Variant(
    string Key,
    string Name,
    [property: JsonPropertyName("rollout_percentage")]
    int RolloutPercentage = 100);

public record FilterProperty(
    string Key,
    string Type,
    JsonElement Value,
    ComparisonType Operator,
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