using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

public class FeatureFlagsResult
{
    public FeatureFlagsConfig Config { get; set; } = new();
    public Dictionary<string, object> ToolbarParams { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public List<string> SupportedCompression { get; set; } = new();
    public ReadOnlyDictionary<string, bool> FeatureFlags { get; set; } = new(new Dictionary<string, bool>());
    public bool SessionRecording { get; set; }
    public bool CaptureDeadClicks { get; set; }
    public CapturePerformance CapturePerformance { get; set; } = new();

    [JsonPropertyName("autocapture_opt_out")]
    public bool AutocaptureOptOut { get; set; }
    public bool AutocaptureExceptions { get; set; }
    public Analytics Analytics { get; set; } = new();
    public bool ElementsChainAsString { get; set; }
    public bool Surveys { get; set; }
    public bool Heatmaps { get; set; }
    public bool DefaultIdentifiedOnly { get; set; }
    public List<object> SiteApps { get; set; } = new();
    public bool ErrorsWhileComputingFlags { get; set; }
    public Dictionary<string, string> FeatureFlagPayloads { get; set; } = new();
}

public class FeatureFlagsConfig
{
    [JsonPropertyName("enable_collect_everything")]
    public bool EnableCollectEverything { get; set; }
}

public class CapturePerformance
{
    [JsonPropertyName("network_timing")]
    public bool NetworkTiming { get; set; }

    [JsonPropertyName("web_vitals")]
    public bool WebVitals { get; set; }

    [JsonPropertyName("web_vitals_allowed_metrics")]
    public object? WebVitalsAllowedMetrics { get; set; }
}

public class Analytics
{
    public string Endpoint { get; set; } = string.Empty;
}
