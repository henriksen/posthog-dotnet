using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PostHog.Api;

/// <summary>
/// An enumeration representing the comparison types that can be used in a filter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ComparisonType>))]
public enum ComparisonType
{
    [JsonStringEnumMemberName("exact")]
    Exact,

    [JsonStringEnumMemberName("in")]
    In,

    [JsonStringEnumMemberName("is_set")]
    IsSet,

    [JsonStringEnumMemberName("eq")]
    Equals,

    [JsonStringEnumMemberName("ne")]
    NotEquals,

    [JsonStringEnumMemberName("gt")]
    GreaterThan,

    [JsonStringEnumMemberName("lt")]
    LessThan,

    [JsonStringEnumMemberName("gte")]
    GreaterThanOrEquals,

    [JsonStringEnumMemberName("lte")]
    LessThanOrEquals,

    [JsonStringEnumMemberName("contains")]
    Contains,

    [JsonStringEnumMemberName("icontains")]
    ContainsIgnoreCase,

    [JsonStringEnumMemberName("regex")]
    Regex,

    [JsonStringEnumMemberName("iregex")]
    RegexIgnoreCase
}