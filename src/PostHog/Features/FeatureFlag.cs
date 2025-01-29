using PostHog.Json;

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

    internal FeatureFlag(string key, StringOrValue<bool> value, IReadOnlyDictionary<string, string>? payloads)
        : this(
            key,
            value.IsString ? value.StringValue is not null : value.Value,
            VariantKey: value.StringValue,
            Payload: payloads?.GetValueOrDefault(key))
    {

    }
}