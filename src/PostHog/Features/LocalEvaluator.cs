using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PostHog.Api;
using PostHog.Json;
using static PostHog.Library.Ensure;

namespace PostHog.Features;

/// <summary>
/// Class used to locally evaluate feature flags.
/// </summary>
public sealed class LocalEvaluator
{
    readonly LocalEvaluationApiResult _flags;
    readonly TimeProvider _timeProvider;
    readonly ILogger<LocalEvaluator> _logger;
    readonly IReadOnlyDictionary<string, FilterSet> _cohortFilters;
    readonly Dictionary<int, string> _groupTypeMapping;

    /// <summary>
    /// Constructs a <see cref="LocalEvaluator"/> with the specified flags.
    /// </summary>
    /// <param name="flags">The flags returned from the local evaluation endpoint.</param>
    /// <param name="timeProvider">The time provider <see cref="TimeProvider"/> to use to determine time.</param>
    /// <param name="logger">The logger</param>
    public LocalEvaluator(
        LocalEvaluationApiResult flags,
        TimeProvider timeProvider,
        ILogger<LocalEvaluator> logger)
    {
        _flags = NotNull(flags);
        _timeProvider = timeProvider;
        _logger = logger;
        _cohortFilters = _flags.Cohorts ?? new Dictionary<string, FilterSet>().AsReadOnly();
        _groupTypeMapping = (_flags.GroupTypeMapping ?? new Dictionary<string, string>())
            .Select(pair => (ConvertGroupTypeIdToInt32(pair.Key), pair.Value))
            .Where(pair => pair.Item1.HasValue)
            .ToDictionary(tuple => tuple.Item1.GetValueOrDefault(), tuple => tuple.Item2);
    }

    /// <summary>
    /// Constructs a <see cref="LocalEvaluator"/> with the specified flags.
    /// </summary>
    /// <param name="flags">The flags returned from the local evaluation endpoint.</param>
    public LocalEvaluator(LocalEvaluationApiResult flags) : this(
        flags,
        TimeProvider.System,
        NullLogger<LocalEvaluator>.Instance)
    {
    }

    /// <summary>
    /// Evaluates whether the specified feature flag matches the specified group and person properties.
    /// </summary>
    /// <remarks>
    /// In PostHog/posthog-python, this would be equivalent to <c>_compute_flag_locally</c>
    /// </remarks>
    /// <param name="key">The feature flag key.</param>
    /// <param name="distinctId">The identifier you use for the user.</param>
    /// <param name="groups">Optional: Context of what groups are related to this event, example: { ["company"] = "id:5" }. Can be used to analyze companies instead of users.</param>
    /// <param name="personProperties">Optional: What person properties are known. Used to compute flags locally.</param>
    /// <param name="warnOnUnknownGroups">Whether to log a warning if the feature flag relies on a group type that's not in the supplied groups.</param>
    /// <returns></returns>
    public StringOrValue<bool>? EvaluateFeatureFlag(
        string key,
        string distinctId,
        GroupCollection? groups = null,
        Dictionary<string, object?>? personProperties = null,
        bool warnOnUnknownGroups = true)
    {
        var flagToEvaluate = _flags.Flags.SingleOrDefault(f => f.Key == key);
        if (flagToEvaluate is null)
        {
            return null;
        }

        return ComputeFlagLocally(
            flagToEvaluate,
            distinctId,
            groups: groups ?? [],
            personProperties ?? [],
            warnOnUnknownGroups);
    }

    StringOrValue<bool> ComputeFlagLocally(
        LocalFeatureFlag flag,
        string distinctId,
        GroupCollection groups,
        Dictionary<string, object?> personProperties,
        bool warnOnUnknownGroups = true)
    {
        if (flag.EnsureExperienceContinuity)
        {
            throw new InconclusiveMatchException($"Flag \"{flag.Key}\" has experience continuity enabled");
        }

        if (!flag.Active)
        {
            return false;
        }

        var filters = flag.Filters;
        var aggregationGroupIndex = filters?.AggregationGroupTypeIndex;

        if (aggregationGroupIndex.HasValue)
        {
            if (!_groupTypeMapping.TryGetValue(aggregationGroupIndex.Value, out var groupType))
            {
                // Weird: We have a group type index that doesn't point to an actual group.
                _logger.LogWarnUnknownGroupType(aggregationGroupIndex.Value, flag.Key);
                throw new InconclusiveMatchException($"Flag has unknown group type index: {aggregationGroupIndex}");
            }

            if (!groups.TryGetGroup(groupType, out var group))
            {
                // Don't failover to `/decide/`, since response will be the same
                if (warnOnUnknownGroups)
                {
                    _logger.LogWarnGroupTypeNotPassedIn(flag.Key);
                }
                else
                {
                    _logger.LogDebugGroupTypeNotPassedIn(flag.Key);
                }

                return false;
            }

            return MatchFeatureFlagProperties(
                flag,
                group.GroupKey,
                group.Properties);
        }

        return MatchFeatureFlagProperties(
            flag,
            distinctId,
            personProperties);
    }

    StringOrValue<bool> MatchFeatureFlagProperties(
        LocalFeatureFlag flag,
        string distinctId,
        Dictionary<string, object?>? properties)
    {
        var filters = flag.Filters;
        var flagConditions = filters?.Groups ?? [];
        bool isInconclusive = false;
        var flagVariants = filters?.Multivariate?.Variants ?? [];

        // Stable sort conditions with variant overrides to the top. This ensures that if overrides are present,
        // they are evaluated first, and the variant override is applied to the first matching condition.
        var sortedConditions = flagConditions.OrderBy(c => c.Variant is not null);

        foreach (var condition in sortedConditions)
        {
            try
            {
                // if any one condition resolves to True, we can short circuit and return
                // the matching variant
                if (IsConditionMatch(flag, distinctId, condition, properties))
                {
                    var variantOverride = condition.Variant;
                    var variant = variantOverride is not null && flagVariants.Select(v => v.Key).Contains(variantOverride)
                        ? variantOverride
                        : GetMatchingVariant(flag, distinctId);

                    return variant is not null
                        ? new StringOrValue<bool>(variant)
                        : true;
                }
            }
            catch (InconclusiveMatchException)
            {
                isInconclusive = true;
            }
        }

        if (isInconclusive)
        {
            throw new InconclusiveMatchException("Can't determine if feature flag is enabled or not with given properties");
        }

        // We can only return False when either all conditions are False, or
        // no condition was inconclusive.
        return false;
    }

    bool IsConditionMatch(
        LocalFeatureFlag flag,
        string distinctId,
        FeatureFlagGroup condition,
        Dictionary<string, object?>? properties)
    {
        var rolloutPercentage = condition.RolloutPercentage;
        if (properties is { Count: > 0 })
        {
            foreach (var property in condition.Properties)
            {
                var isMatch = property.Type is FilterType.Cohort
                    ? MatchCohort(property, properties)
                    : MatchProperty(property, properties);
                if (!isMatch)
                {
                    return false;
                }
            }

            if (rolloutPercentage is 100)
            {
                return true;
            }
        }

        var hashValue = Hash(flag.Key, distinctId);
        return !(hashValue > rolloutPercentage / 100.0);
    }

    static string? GetMatchingVariant(LocalFeatureFlag flag, string distinctId)
    {
        var hashValue = Hash(flag.Key, distinctId, salt: "variant");
        return CreateVariaGetVariantLookupTable(flag)
            .FirstOrDefault(variant => hashValue >= variant.MinValue && hashValue < variant.MaxValue)
            ?.Key;
    }


    record VariantRange(string Key, double MinValue, double MaxValue);

    static List<VariantRange> CreateVariaGetVariantLookupTable(LocalFeatureFlag flag)
    {
        List<VariantRange> results = new();
        var multivariates = flag.Filters?.Multivariate?.Variants;
        if (multivariates is null)
        {
            return results;
        }
        double minValue = 0;
        foreach (var variant in multivariates)
        {
            var maxValue = minValue + variant.RolloutPercentage / 100.0;
            results.Add(new VariantRange(variant.Key, minValue, maxValue));
            minValue = maxValue;
        }

        return results;
    }

    bool MatchCohort(
        PropertyFilter filter,
        Dictionary<string, object?> propertyValues)
    {
        // Cohort properties are in the form of property groups like this:
        // {
        //     "cohort_id": {
        //         "type": "AND|OR",
        //         "values": [{
        //            "key": "property_name", "value": "property_value"
        //        }]
        //     }
        // }
        var cohortId = filter.Value.StringValue;
        if (cohortId is null || !_cohortFilters.TryGetValue(cohortId, out var conditions))
        {
            throw new InconclusiveMatchException("can't match cohort without a given cohort property value");
        }

        return MatchPropertyGroup(conditions, propertyValues);
    }

    bool MatchPropertyGroup(
        FilterSet? filterSet,
        Dictionary<string, object?> propertyValues)
    {
        if (filterSet is null)
        {
            return true;
        }

        var filters = filterSet.Values;
        if (filters is null or [])
        {
            // Empty groups are no-ops, always match
            return true;
        }

        bool errorMatchingLocally = false;

        // Test the first element to see what type of filters we're dealing with here.
        var isFilterSet = filters[0] is FilterSet;

        if (isFilterSet)
        {
            // A nested property group.
            // We expect every filter to be a filter set. At least this is how the other client libraries work.
            foreach (var filter in filters)
            {
                var childFilterSet = filter as FilterSet;
                if (childFilterSet is null)
                {
                    continue;
                }

                try
                {
                    var isMatch = MatchPropertyGroup(childFilterSet, propertyValues);
                    if (childFilterSet.Type is FilterType.And)
                    {
                        if (!isMatch)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // OR group
                        if (isMatch)
                        {
                            return true;
                        }
                    }
                }
                catch (InconclusiveMatchException e)
                {
                    _logger.LogDebugFailedToComputeProperty(e, filter);
                    errorMatchingLocally = true;
                }
            }

            if (errorMatchingLocally)
            {
                throw new InconclusiveMatchException("Can't match cohort without a given cohort property value");
            }

            // if we get here, all matched in AND case, or none matched in OR case
            return filterSet.Type is FilterType.And;
        }

        foreach (var filter in filters)
        {
            var propertyFilter = filter as PropertyFilter;
            if (propertyFilter is null)
            {
                continue;
            }
            try
            {
                var isMatch = filter.Type is FilterType.Cohort
                    ? MatchCohort(propertyFilter, propertyValues)
                    : MatchProperty(propertyFilter, propertyValues);
                var negation = propertyFilter.Negation;
                if (filterSet.Type is FilterType.And)
                {
                    if (!isMatch && !negation)
                    {
                        // If negated property, do the inverse
                        return false;
                    }

                    if (isMatch && negation)
                    {
                        return false;
                    }
                }
                else // OR Group
                {
                    if (isMatch && !negation)
                    {
                        return true;
                    }

                    if (!isMatch && negation)
                    {
                        return true;
                    }
                }

            }
            catch (InconclusiveMatchException e)
            {
                _logger.LogDebugFailedToComputeProperty(e, filter);
                errorMatchingLocally = true;
            }
        }

        if (errorMatchingLocally)
        {
            throw new InconclusiveMatchException("Can't match cohort without a given cohort property value");
        }

        // if we get here, all matched in AND case, or none matched in OR case
        return filterSet.Type is FilterType.And;
    }

    /// <summary>
    /// Evaluates a feature flag for a given set of properties.
    /// </summary>
    /// <remarks>
    /// Only looks for matches where the key exists in properties.
    /// Doesn't support the operator <c>is_not_set</c>.
    /// </remarks>
    /// <param name="propertyFilter">The <see cref="PropertyFilter"/> to evaluate.</param>
    /// <param name="properties">The overriden values that describe the user/group.</param>
    /// <returns><c>true</c> if the current user/group matches the property. Otherwise <c>false</c>.</returns>
    bool MatchProperty(PropertyFilter propertyFilter, Dictionary<string, object?> properties)
    {
        var key = NotNull(propertyFilter.Key);
        if (propertyFilter.Value is not { } propertyValue)
        {
            throw new InconclusiveMatchException("The filter property value is null");
        }
        var value = propertyValue ?? throw new InconclusiveMatchException("The filter property value is null");

        // The overrideValue is the value that the user or group has set for the property. It's called "override value"
        // because when passing it to the `/decide` endpoint, it overrides the values stored in PostHog. For local
        // evaluation, it's a bit of a misnomer because this is the *only* value we're concerned with. I thought about
        // naming this to comparand but wanted to keep the naming consistent with the other client libraries.
        // @haacked
        if (!NotNull(properties).TryGetValue(key, out var overrideValue))
        {
            throw new InconclusiveMatchException("Can't match properties without a given property value");
        }

        if (overrideValue is null && propertyFilter.Operator != ComparisonOperator.IsNot)
        {
            // If the value is null, just fail the feature flag comparison. This doesn't throw an
            // InconclusiveMatchException because the property value was provided.
            return false;
        }

        return propertyFilter.Operator switch
        {
            ComparisonOperator.Exact => value.IsExactMatch(overrideValue),
            ComparisonOperator.IsNot => !value.IsExactMatch(overrideValue),
            ComparisonOperator.GreaterThan => value > overrideValue,
            ComparisonOperator.LessThan => value < overrideValue,
            ComparisonOperator.GreaterThanOrEquals => value >= overrideValue,
            ComparisonOperator.LessThanOrEquals => value <= overrideValue,
            ComparisonOperator.ContainsIgnoreCase => value.Contains(overrideValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.DoesNotContainsIgnoreCase => !value.Contains(overrideValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.Regex => value.IsRegexMatch(overrideValue),
            ComparisonOperator.NotRegex => !value.IsRegexMatch(overrideValue),
            ComparisonOperator.IsSet => true, // We already checked to see that the key exists.
            ComparisonOperator.IsDateBefore => value.IsDateBefore(overrideValue, _timeProvider.GetUtcNow()),
            ComparisonOperator.IsDateAfter => !value.IsDateBefore(overrideValue, _timeProvider.GetUtcNow()),
            _ => throw new InconclusiveMatchException($"Unknown operator: {propertyFilter.Operator}")
        };
    }

    int? ConvertGroupTypeIdToInt32(string id)
    {
        if (int.TryParse(id, out var intId))
        {
            return intId;
        }

        _logger.LogErrorInvalidGroupIdSkipped(id);
        return null;
    }

    // This function takes a distinct_id and a feature flag key and returns a float between 0 and 1.
    // Given the same distinct_id and key, it'll always return the same float. These floats are
    // uniformly distributed between 0 and 1, so if we want to show this feature to 20% of traffic
    // we can do _hash(key, distinct_id) < 0.2
    // Ported from https://github.com/PostHog/posthog-python/blob/master/posthog/feature_flags.py#L23C1-L30
    static double Hash(string key, string distinctId, string salt = "")
    {
        var hashKey = $"{key}.{distinctId}{salt}";
#pragma warning disable CA5350 // This SHA is not used for security purposes
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(hashKey));
#pragma warning restore CA5350

        // Convert the first 15 characters of the hex representation to an integer
        var hexString = Convert.ToHexString(hashBytes)[..15];
        var hashVal = Convert.ToUInt64(hexString, 16);

        // Ensure the value is within the correct range (60 bits)
        hashVal &= 0xFFFFFFFFFFFFFFF;

        return hashVal / LongScale;
    }

    const double LongScale = 0xFFFFFFFFFFFFFFF;
}

internal static partial class LocalEvaluatorLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "[FEATURE FLAGS] Unknown group type index {AggregationGroupIndex} for feature flag {FlagKey}")]
    public static partial void LogWarnUnknownGroupType(
        this ILogger<LocalEvaluator> logger,
        int aggregationGroupIndex,
        string flagKey);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "[FEATURE FLAGS] Can't compute group feature flag: {FlagKey} without group types passed in")]
    public static partial void LogDebugGroupTypeNotPassedIn(
        this ILogger<LocalEvaluator> logger,
        string flagKey);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "[FEATURE FLAGS] Can't compute group feature flag: {FlagKey} without group types passed in")]
    public static partial void LogWarnGroupTypeNotPassedIn(
        this ILogger<LocalEvaluator> logger,
        string flagKey);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Failed to compute property {Property} locally")]
    public static partial void LogDebugFailedToComputeProperty(
        this ILogger<LocalEvaluator> logger,
        Exception e,
        Filter property);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Error,
        Message = "Group Type mapping has an invalid group type id: {GroupTypeId}. Skipping it.")]
    public static partial void LogErrorInvalidGroupIdSkipped(
        this ILogger<LocalEvaluator> logger,
        string groupTypeId);
}