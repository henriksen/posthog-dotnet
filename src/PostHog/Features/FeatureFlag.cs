using PostHog.Json;
using static PostHog.Library.Ensure;

namespace PostHog.Features;

/// <summary>
/// Represents a feature flag.
/// </summary>
/// <param name="Key">The feature flag key.</param>
/// <param name="IsEnabled"><c>true</c> if the feature is enabled for the current user, otherwise <c>false</c>.</param>
/// <param name="VariantKey">For multivariate feature flags, this is the key enabled for the user.</param>
/// <param name="Payload">The payload for the flag or the variant.</param>
public record FeatureFlag(
    string Key,
    bool IsEnabled,
    string? VariantKey = null,
    string? Payload = null)
{
    /// <summary>
    /// Constructs a new instance of <see cref="FeatureFlag"/>.
    /// </summary>
    /// <param name="kvp">A key value pair with the feature key and the resulting value.</param>
    /// <param name="payloads">The payload dictionary from the API response.</param>
    public FeatureFlag(KeyValuePair<string, StringOrValue<bool>> kvp, IReadOnlyDictionary<string, string>? payloads)
        : this(kvp.Key, kvp.Value, payloads)
    {
    }

    internal FeatureFlag(string key, StringOrValue<bool> value, IReadOnlyDictionary<string, string>? payloads = null)
        : this(
            key,
            value.IsString ? value.StringValue is not null : value.Value,
            VariantKey: value.StringValue,
            Payload: payloads?.GetValueOrDefault(key))
    {
    }

    /// <summary>
    /// Implicit cast to nullable boolean.
    /// </summary>
    /// <param name="flag">The <see cref="FeatureFlag"/>.</param>
    /// <returns><c>true</c> if this feature flag is enabled, <c>false</c> if it is not, and <c>null</c> if it can't be determined.</returns>
#pragma warning disable CA2225
    public static implicit operator bool(FeatureFlag? flag) => flag is { IsEnabled: true };
#pragma warning restore CA2225

    /// <summary>
    /// Implicit cast to string. This returns the variant key.
    /// </summary>
    /// <param name="flag">The <see cref="FeatureFlag"/>.</param>
    /// <returns>The variant key, if this flag is enabled and has a variant key, otherwise the IsEnabled value as a string.</returns>
    public static implicit operator string(FeatureFlag? flag) => flag?.VariantKey ?? ((bool)flag).ToString();
}