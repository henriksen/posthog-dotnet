using System.Text.Json.Serialization;
using PostHog.Json;
using PostHog.Library;

namespace PostHog.Api;

/// <summary>
/// The API Payload from the <c>/api/feature_flag/local_evaluation</c> endpoint used to evaluate feature flags
/// locally.
/// </summary>
/// <param name="Flags">The list of feature flags.</param>
/// <param name="GroupTypeMapping">A mapping of group IDs to group type.</param>
/// <param name="Cohorts">A mapping of cohort IDs to a set of filters.</param>
public record LocalEvaluationApiResult(
    IReadOnlyList<LocalFeatureFlag> Flags,
    [property: JsonPropertyName("group_type_mapping")]
    IReadOnlyDictionary<string, string>? GroupTypeMapping = null,
    IReadOnlyDictionary<string, FilterSet>? Cohorts = null)
{
    public virtual bool Equals(LocalEvaluationApiResult? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Flags.ListsAreEqual(other.Flags)
               && GroupTypeMapping.DictionariesAreEqual(other.GroupTypeMapping)
               && Cohorts.DictionariesAreEqual(other.Cohorts);
    }

    public override int GetHashCode() => HashCode.Combine(Flags, GroupTypeMapping, Cohorts);
}

public record LocalFeatureFlag(
    int Id,
    [property: JsonPropertyName("team_id")]
    int TeamId,
    string Name,
    string Key,
    FeatureFlagFilters? Filters = null,
    bool Deleted = false,
    bool Active = true,
    [property: JsonPropertyName("ensure_experience_continuity")]
    bool EnsureExperienceContinuity = false);

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
    IReadOnlyList<FeatureFlagGroup>? Groups,
    IReadOnlyDictionary<string, string>? Payloads = null,
    Multivariate? Multivariate = null,
    [property: JsonPropertyName("aggregation_group_type_index")]
    int? AggregationGroupTypeIndex = null)
{
    public virtual bool Equals(FeatureFlagFilters? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Groups.ListsAreEqual(other.Groups)
               && Payloads.DictionariesAreEqual(other.Payloads)
               && Multivariate == other.Multivariate
               && AggregationGroupTypeIndex == other.AggregationGroupTypeIndex;
    }

    public override int GetHashCode() => HashCode.Combine(Groups, Payloads, Multivariate, AggregationGroupTypeIndex);
}

/// <summary>
/// Set of conditions that determine who sees the feature flag. If any group matches, the flag is active for that user.
/// </summary>
/// <param name="Variant">Optional override to serve a specific variant to users matching this group.</param>
/// <param name="Properties">Conditions about the user/group. (e.g. "user is in country X" or "user is in cohort Y")</param>
/// <param name="RolloutPercentage">Optional percentage (0-100) for gradual rollouts. Defaults to 100.</param>
public record FeatureFlagGroup(
    IReadOnlyList<PropertyFilter> Properties,
    string? Variant = null,
    [property: JsonPropertyName("rollout_percentage")]
    int RolloutPercentage = 100)
{
    public virtual bool Equals(FeatureFlagGroup? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Properties.SequenceEqual(other.Properties)
               && Variant == other.Variant
               && RolloutPercentage == other.RolloutPercentage;
    }

    public override int GetHashCode() => HashCode.Combine(Properties, Variant, RolloutPercentage);
}

public record Multivariate(IReadOnlyCollection<Variant> Variants)
{
    public virtual bool Equals(Multivariate? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Variants.SequenceEqual(other.Variants);
    }

    public override int GetHashCode() => Variants.GetHashCode();
}

public record Variant(
    string Key,
    string Name,
    [property: JsonPropertyName("rollout_percentage")]
    double RolloutPercentage = 100);

/// <summary>
/// Base class for <see cref="FilterSet"/> or <see cref="PropertyFilter"/>.
/// </summary>
/// <param name="Type">
/// The type of filter. For <see cref="FilterSet"/>, it'll be "OR" or "AND".
/// For <see cref="PropertyFilter"/> it'll be "person" or "group".
/// </param>
[JsonConverter(typeof(FilterJsonConverter))]
public abstract record Filter(FilterType Type);

/// <summary>
/// A grouping ("AND" or "OR")
/// </summary>
/// <param name="Type">The type of filter. Either "AND" or "OR".</param>
/// <param name="Values">A collection of filters to evaluate. Allows for nesting.</param>
public record FilterSet(FilterType Type, IReadOnlyList<Filter> Values) : Filter(Type)
{
    public virtual bool Equals(FilterSet? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type
               && Values.ListsAreEqual(other.Values);
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Values);
}

/// <summary>
/// A filter that filters on a property.
/// </summary>
/// <param name="Type">The type of filter. Either "person" or "group".</param>
public record PropertyFilter(
    FilterType Type,
    string Key,
    PropertyFilterValue Value,
    ComparisonOperator? Operator = null,
    [property: JsonPropertyName("group_type_index")]
    int? GroupTypeIndex = null,
    bool Negation = false) : Filter(Type)
{
    public virtual bool Equals(PropertyFilter? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type == other.Type
               && Key == other.Key
               && Value.Equals(other.Value)
               && Operator == other.Operator
               && GroupTypeIndex == other.GroupTypeIndex
               && Negation == other.Negation;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), Key, Value, Operator, GroupTypeIndex, Negation);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<FilterType>))]
public enum FilterType
{
    [JsonStringEnumMemberName("person")]
    Person,

    [JsonStringEnumMemberName("group")]
    Group,

    [JsonStringEnumMemberName("cohort")]
    Cohort,

    [JsonStringEnumMemberName("OR")]
    Or,

    [JsonStringEnumMemberName("AND")]
    And
}
