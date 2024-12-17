using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Library;

namespace PostHog.FeatureFlags;

/// <summary>
/// API Client for the Feature Flags endpoint.
/// </summary>
/// <param name="projectApiKey">Your project API key.</param>
/// <param name="hostUrl">
/// <c>us.i.posthog.com</c> for US Cloud. <c>eu.i.posthog.com</c> for EU Cloud.
/// Your custom domain for self-hosted.
/// </param>
public class FeatureFlagsClient(string projectApiKey, Uri hostUrl)
{
    public FeatureFlagsClient(string projectApiKey)
        : this(projectApiKey, new Uri("https://us.i.posthog.com"))
    {
    }

    async Task<FeatureFlagsResult> RequestFeatureFlagsAsync(string distinctUserId, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var endpointUrl = new Uri($"{hostUrl}/decide?v=3");

        var requestBody = new Dictionary<string, string>
        {
            ["api_key"] = projectApiKey,
            ["distinct_id"] = distinctUserId,
            ["$lib"] = "posthog-dotnet"
        };

        return await httpClient.PostJsonAsync<FeatureFlagsResult>(endpointUrl, requestBody, cancellationToken)
            ?? new FeatureFlagsResult();
    }

    public async Task<FeatureFlagsEvaluator> GetFeatureFlagsAsync(
        string distinctUserId,
        CancellationToken cancellationToken)
    {
        var result = await RequestFeatureFlagsAsync(distinctUserId, cancellationToken);
        return new FeatureFlagsEvaluator(result);
    }
}

