using Microsoft.Extensions.Options;
using PostHog.Api;
using PostHog.Config;
using PostHog.Library;

namespace PostHog.Features;

/// <summary>
/// This class is responsible for loading the feature flags from the PostHog API and storing them locally.
/// It polls the API at a regular interval (set in <see cref="PostHogOptions"/>) and stores the result in memory.
/// </summary>
/// <param name="postHogApiClient">The <see cref="IPostHogApiClient"/> used to make requests.</param>
/// <param name="options">The options used to configure the client.</param>
/// <param name="timeProvider">The time provider <see cref="TimeProvider"/> to use to determine time.</param>
/// <param name="taskScheduler">Used to run tasks on the background.</param>
public sealed class LocalFeatureFlagsLoader(
    IPostHogApiClient postHogApiClient,
    IOptions<PostHogOptions> options,
    ITaskScheduler taskScheduler,
    TimeProvider timeProvider) : IDisposable
{
    volatile LocalEvaluationApiResult? _localEvaluationApiResult;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly PeriodicTimer _timer = new(options.Value.FeatureFlagPollInterval, timeProvider);

    public void Start()
    {
        taskScheduler.Run(() => PollForFeatureFlagsAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Retrieves the feature flags from the local cache. If the cache is empty, it will fetch the flags from the API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
    /// <returns>All the feature flags.</returns>
    public async ValueTask<LocalEvaluationApiResult?> GetFeatureFlagsForLocalEvaluationAsync(CancellationToken cancellationToken)
    {
        if (_localEvaluationApiResult is { } result)
        {
            return result;
        }
        return await LoadEvaluationResultAsync(cancellationToken);
    }

    async Task<LocalEvaluationApiResult?> LoadEvaluationResultAsync(CancellationToken cancellationToken)
    {
        var newApiResult = await postHogApiClient.GetFeatureFlagsForLocalEvaluationAsync(cancellationToken);
        if (newApiResult is null)
        {
            return _localEvaluationApiResult;
        }
        Interlocked.Exchange(ref _localEvaluationApiResult, newApiResult);
        return _localEvaluationApiResult;
    }

    async Task PollForFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await LoadEvaluationResultAsync(cancellationToken);
            await _timer.WaitForNextTickAsync(cancellationToken);
        }
    }

    internal LocalEvaluationApiResult? CachedValue => _localEvaluationApiResult;

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
        _timer.Dispose();
    }
}