namespace PostHog.FeatureFlags;

/// <summary>
/// Represents the result of a Feature Flags request. Used to evaluate whether a feature
/// flag is present.
/// </summary>
/// <param name="featureFlagsResult"></param>
public class FeatureFlagsEvaluator(FeatureFlagsResult featureFlagsResult)
{
    public bool IsFeatureEnabled(string featureKey) =>
        featureFlagsResult.FeatureFlags.TryGetValue(featureKey, out var result)
        && result.Value;
}
