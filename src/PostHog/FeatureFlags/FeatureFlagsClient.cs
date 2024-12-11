using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PostHog.Json;

namespace PostHog.FeatureFlags;

/// <summary>
/// API Client for the Feature Flags endpoint.
/// </summary>
/// <param name="projectApiKey">Your project API key.</param>
/// <param name="hostUrl">
/// <c>us.i.posthog.com</c> for US Cloud. <c>eu.i.posthog.com</c> for EU Cloud.
/// Your custom domain for self-hosted.
/// </param>
public class FeatureFlagsClient(string projectApiKey, string hostUrl = "https://us.i.posthog.com")
{
    async Task<FeatureFlagsResult> RequestFeatureFlagsAsync(string distinctUserId)
    {
        using var httpClient = new HttpClient();
        var endpointUrl = $"{hostUrl}/decide?v=3";
        var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);

        var requestBody = new Dictionary<string, string>
        {
            { "api_key", projectApiKey },
            { "distinct_id", distinctUserId }
        };
        var json = JsonSerializer.Serialize(requestBody);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Add("Authorization", $"Bearer {projectApiKey}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseStream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializerHelper.DeserializeFromCamelCaseJsonAsync<FeatureFlagsResult>(responseStream)
               ?? new();
    }

    public async Task<FeatureFlagsCollection> GetFeatureFlagsAsync(string distinctUserId)
    {
        var result = await RequestFeatureFlagsAsync(distinctUserId);
        return new FeatureFlagsCollection(result);
    }
}

