using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostHog.Models;

namespace PostHog;

/// <summary>
/// A hosted <see cref="IPostHogClient" /> designed to be easily registered with the DI container.
/// </summary>
public sealed class PostHogHostedClient : IPostHogClient
{
    readonly PostHogClient _innerClient;

    public PostHogHostedClient(IOptions<PostHogOptions> options, ILogger<PostHogClient> logger)
    {
        options = options ?? throw new ArgumentNullException(nameof(options));
        _innerClient = new PostHogClient(options.Value, logger);
    }

    public void Dispose() => _innerClient.Dispose();

    public ValueTask DisposeAsync() => _innerClient.DisposeAsync();

    public Task<ApiResult> IdentifyAsync(string distinctId, Dictionary<string, object> userProperties, CancellationToken cancellationToken)
        => _innerClient.IdentifyAsync(distinctId, userProperties, cancellationToken);

    public void Capture(string distinctId, string eventName, Dictionary<string, object>? properties)
        => _innerClient.Capture(distinctId, eventName, properties);

    public Task FlushAsync() => _innerClient.FlushAsync();
}