using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PostHog.Models;

namespace PostHog;

/// <summary>
/// Interface for the PostHog client. This is the main interface for interacting with PostHog.
/// Use this to identify users and capture events.
/// </summary>
public interface IPostHogClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="distinctId"></param>
    /// <param name="userProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ApiResult> IdentifyAsync(
        string distinctId,
        Dictionary<string, object> userProperties,
        CancellationToken cancellationToken);

    void Capture(
        string distinctId,
        string eventName,
        Dictionary<string, object>? properties);

    Task FlushAsync();
}