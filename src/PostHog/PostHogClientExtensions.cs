using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Models;

namespace PostHog;

public static class PostHogClientExtensions
{
    public static Task<ApiResult> IdentifyAsync(
        this IPostHogClient client,
        string distinctId,
        CancellationToken cancellationToken) =>
        (client ?? throw new ArgumentNullException(nameof(client)))
            .IdentifyAsync(distinctId, new Dictionary<string, object>(), cancellationToken);
}