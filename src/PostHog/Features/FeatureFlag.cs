namespace PostHog.Features;

/// <summary>
/// Represents a feature flag.
/// </summary>
/// <param name="Key">The feature flag key.</param>
/// <param name="IsEnabled">Whether or not it is enabled for the current user.</param>
/// <param name="VariantKey">For multivariate feature flags, this is the key enabled for the user.</param>
/// <param name="Payload">The payload for the flag or the variant.</param>
public record FeatureFlag(
    string Key,
    bool IsEnabled,
    string? VariantKey,
    string? Payload);