using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using PostHog.Json;

namespace PostHog.Api;

public class FeatureFlagsApiResult
{
    public FeatureFlagsConfig Config { get; set; } = new(false);
    public ReadOnlyDictionary<string, object> ToolbarParams { get; set; } = new(new Dictionary<string, object>());
    public bool IsAuthenticated { get; set; }
    public ReadOnlyCollection<string> SupportedCompression { get; set; } = new(Array.Empty<string>());
    public ReadOnlyDictionary<string, StringOrValue<bool>> FeatureFlags { get; set; } = new(new Dictionary<string, StringOrValue<bool>>());
    public bool SessionRecording { get; set; }
    public bool CaptureDeadClicks { get; set; }
    public CapturePerformance CapturePerformance { get; set; } = new(false, false, null);

    [JsonPropertyName("autocapture_opt_out")]
    public bool AutocaptureOptOut { get; set; }
    public bool AutocaptureExceptions { get; set; }
    public Analytics Analytics { get; set; } = new(string.Empty);
    public bool ElementsChainAsString { get; set; }
    public bool Surveys { get; set; }
    public bool Heatmaps { get; set; }
    public bool DefaultIdentifiedOnly { get; set; }
    public ReadOnlyCollection<object> SiteApps { get; set; } = new(Array.Empty<object>());
    public bool ErrorsWhileComputingFlags { get; set; }
    public ReadOnlyDictionary<string, string> FeatureFlagPayloads { get; set; } = new(new Dictionary<string, string>());
}

public class FeatureFlagsConfig(bool enableCollectEverything)
{
    [JsonPropertyName("enable_collect_everything")]
    public bool EnableCollectEverything => enableCollectEverything;
}

public class CapturePerformance(
    bool networkTiming,
    bool webVitals,
    object? webVitalsAllowedMetrics)
{
    [JsonPropertyName("network_timing")]
    public bool NetworkTiming => networkTiming;

    [JsonPropertyName("web_vitals")]
    public bool WebVitals => webVitals;

    [JsonPropertyName("web_vitals_allowed_metrics")]
    public object? WebVitalsAllowedMetrics => webVitalsAllowedMetrics;
}

public class Analytics(string endpoint)
{
    public string Endpoint => endpoint;
}
