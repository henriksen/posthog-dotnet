using System.Net;
using System.Net.Http.Headers;

namespace PostHog.Library;

/// <summary>
/// A <see cref="DelegatingHandler"/> used to log HTTP requests and responses. This is useful for debugging
/// API calls.
/// </summary>
/// <param name="logger">The logger.</param>
public class LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log request
        var requestBody = request.Content != null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : string.Empty;
        logger.LogTraceRequest(request.Method, request.RequestUri, FormatHeaders(request.Headers), requestBody);

        var response = await base.SendAsync(request, cancellationToken);

        // Log response
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogTraceResponse(response.StatusCode, FormatHeaders(response.Headers), responseBody);

        return response;
    }

    static string FormatHeaders(HttpHeaders headers) =>
        headers.Aggregate(
            "\n",
            (text, header) => text + $"{header.Key}: {string.Join(", ", header.Value)}\n");
}

internal static partial class LoggingHttpMessageHandlerLoggerExtensions
{
    [LoggerMessage(
        EventId = 400,
        Level = LogLevel.Trace,
        Message = "HTTP Request: {Method} {RequestUri}{Headers}{Body}")]
    public static partial void LogTraceRequest(
        this ILogger<LoggingHttpMessageHandler> logger,
        HttpMethod method,
        Uri? requestUri,
        string headers,
        string body);

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Trace,
        Message = "HTTP Response: {StatusCode}{Headers}{Body}")]
    public static partial void LogTraceResponse(
        this ILogger<LoggingHttpMessageHandler> logger,
        HttpStatusCode statusCode,
        string headers,
        string body);
}