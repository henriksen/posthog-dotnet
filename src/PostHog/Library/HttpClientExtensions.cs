using System.Net.Http.Json;
using PostHog.Json;

namespace PostHog.Library;

/// <summary>
/// Extension methods for <see cref="HttpClient"/>.
/// </summary>
internal static class HttpClientExtensions
{
    /// <summary>
    /// Sends a POST request to the specified Uri containing the value serialized as JSON in the request body.
    /// Returns the response body deserialized as <typeparamref name="TBody"/>.
    /// </summary>
    /// <param name="httpClient">The client used to send the request.</param>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="content">The value to serialize.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <typeparam name="TBody">The type of the response body to deserialize to.</typeparam>
    /// <returns>The task representing the asynchronous operation.</returns>
    public static async Task<TBody?> PostJsonAsync<TBody>(
        this HttpClient httpClient,
        Uri requestUri,
        object content,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            requestUri,
            content,
            JsonSerializerHelper.Options,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializerHelper.DeserializeFromCamelCaseJsonAsync<TBody>(
            result,
            cancellationToken: cancellationToken);
    }
}