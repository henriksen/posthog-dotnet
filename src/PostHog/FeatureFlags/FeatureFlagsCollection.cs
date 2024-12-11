namespace PostHog.FeatureFlags;

public class FeatureFlagsCollection(FeatureFlagsResult featureFlagsResult)
{
    public bool IsFeatureEnabled(string featureKey) =>
        featureFlagsResult.FeatureFlags.TryGetValue(featureKey, out var result)
        && result;
}
