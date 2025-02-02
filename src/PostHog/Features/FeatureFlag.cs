using PostHog.Api;
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
    /// Creates a <see cref="FeatureFlag"/> instance as a result of the /decide endpoint response. Since payloads are
    /// already calculated, we can look them up by the feature key.
    /// </summary>
    /// <param name="key">The feature flag key.</param>
    /// <param name="value">The value of the flag.</param>
    /// <param name="apiResult">The Decide Api result.</param>
    public static FeatureFlag CreateFromDecide(
        string key,
        StringOrValue<bool> value,
        DecideApiResult apiResult)
    {
        var payload = NotNull(apiResult).FeatureFlagPayloads?.GetValueOrDefault(key);
        return new FeatureFlag(key, value, payload);
    }

    /// <summary>
    /// Creates a <see cref="FeatureFlag"/> instance as part of local evaluation. It makes sure to look up the
    /// payload based on the value of the feature flag.
    /// </summary>
    /// <param name="key">The feature flag key.</param>
    /// <param name="value">The value of the flag.</param>
    /// <param name="localFeatureFlag">The feature flag definition.</param>
    public static FeatureFlag CreateFromLocalEvaluation(
        string key,
        StringOrValue<bool> value,
        LocalFeatureFlag localFeatureFlag)
    {
#pragma warning disable CA1308
        var payloadKey = value.StringValue ?? value.Value.ToString().ToLowerInvariant();
#pragma warning restore CA1308
        return new FeatureFlag(key, value, NotNull(localFeatureFlag).Filters?.Payloads?.GetValueOrDefault(payloadKey));
    }

    FeatureFlag(string key, StringOrValue<bool> value, string? payload)
        : this(
            key,
            value.IsString ? value.StringValue is not null : value.Value,
            VariantKey: value.StringValue,
            Payload: payload)
    {
    }

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
#pragma warning disable CA1308 // We gotta match what PostHog sends us
            Payload: payloads?.GetValueOrDefault(value.StringValue ?? value.Value.ToString().ToLowerInvariant()))
#pragma warning restore CA1308
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