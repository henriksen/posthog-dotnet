using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using PostHog.Json;

namespace PostHog.Api;

/// <summary>
/// The API Result for the <c>/decide</c> endpoint. Some properties are ommitted because
/// they are not necessary for server-side scenarios.
/// </summary>
/// <remarks>
///
/// </remarks>
public class DecideApiResult
{
    public FeatureFlagsConfig Config { get; init; } = new(false);
    public bool IsAuthenticated { get; init; }
    public ReadOnlyDictionary<string, StringOrValue<bool>> FeatureFlags { get; init; } = new(new Dictionary<string, StringOrValue<bool>>());
    public Analytics Analytics { get; init; } = new(string.Empty);
    public bool DefaultIdentifiedOnly { get; init; }
    public bool ErrorsWhileComputingFlags { get; init; }
    public ReadOnlyDictionary<string, string> FeatureFlagPayloads { get; init; } = new(new Dictionary<string, string>());
}

public class FeatureFlagsConfig(bool enableCollectEverything)
{
    [JsonPropertyName("enable_collect_everything")]
    public bool EnableCollectEverything => enableCollectEverything;
}

public class Analytics(string endpoint)
{
    public string Endpoint => endpoint;
}
