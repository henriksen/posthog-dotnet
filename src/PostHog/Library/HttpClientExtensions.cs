#define USE_STREAM_CONTENT

using System;
using System.Net.Http;

#if !USE_STREAM_CONTENT
using System.Net.Http.Headers;
#endif
#if USE_STREAM_CONTENT
using System.Text;
#endif
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
#if USE_STREAM_CONTENT
        var json = await JsonSerializerHelper.SerializeToCamelCaseJsonStringAsync(content);
        using var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(requestUri, stringContent, cancellationToken);
        response.EnsureSuccessStatusCode();
#else
        var jsonStream = await JsonSerializerHelper.SerializeToCamelCaseJsonStreamAsync(content);
        jsonStream.Position = 0;
        using var streamContent = new StreamContent(jsonStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await httpClient.PostAsync(requestUri, streamContent, cancellationToken);
        response.EnsureSuccessStatusCode();
#endif

#if USE_STREAM_CONTENT
        var result = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializerHelper.DeserializeFromCamelCaseJsonAsync<TBody>(
            result,
            cancellationToken: cancellationToken);
#else
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return await JsonSerializerHelper.DeserializeFromCamelCaseJsonStringAsync<TBody>(result);
#endif
    }
}