using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Json;

namespace PostHog.Library;

internal static class HttpClientExtensions
{
    public static async Task<TBody?> PostJsonAsync<TBody>(
        this HttpClient httpClient,
        Uri requestUri,
        object content,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(content);
        using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(requestUri, stringContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializerHelper.DeserializeFromCamelCaseJsonAsync<TBody>(
            result,
            cancellationToken: cancellationToken);
    }
}