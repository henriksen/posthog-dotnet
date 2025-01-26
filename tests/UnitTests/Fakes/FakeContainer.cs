using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PostHog;
using PostHog.Api;
using PostHog.Config;
using PostHog.Library;

namespace UnitTests.Fakes;

#pragma warning disable CA2000

/// <summary>
/// A fake container for dependency injection in some unit tests. For now this is really dumb.
/// </summary>
public class FakeContainer : IServiceProvider
{
    readonly Dictionary<Type, Func<IServiceProvider, object?>> _services = new();

    public FakeContainer()
    {
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        AddService<HttpMessageHandler>(new FakeHttpMessageHandler());
        AddService<TimeProvider>(timeProvider);
        AddService<ITaskScheduler>(new FakeTaskScheduler());
        AddService<ILogger<PostHogApiClient>>(new NullLogger<PostHogApiClient>());
        AddService<ILogger<PostHogClient>>(new NullLogger<PostHogClient>());
        AddService<IOptions<PostHogOptions>>(new PostHogOptions { ProjectApiKey = "fake-project-api-key" });
        AddService<HttpClient>(new HttpClient(this.GetRequiredService<HttpMessageHandler>()));
        AddService<IFeatureFlagCache>(NullFeatureFlagCache.Instance);

        // Add the PostHogClient and PostHogApiClient services
        AddService<IPostHogApiClient, PostHogApiClient>(sp => new PostHogApiClient(
            httpClient: sp.GetRequiredService<HttpClient>(),
            authenticatedHttpClient: sp.GetRequiredService<HttpClient>(),
            options: sp.GetRequiredService<IOptions<PostHogOptions>>().Value,
            timeProvider: sp.GetRequiredService<TimeProvider>(),
            logger: sp.GetRequiredService<ILogger<PostHogApiClient>>()));
        AddService<IPostHogClient, PostHogClient>(sp => new PostHogClient(
            postHogApiClient: sp.GetRequiredService<IPostHogApiClient>(),
            featureFlagsCache: sp.GetRequiredService<IFeatureFlagCache>(),
            options: sp.GetRequiredService<IOptions<PostHogOptions>>().Value,
            taskScheduler: sp.GetRequiredService<ITaskScheduler>(),
            timeProvider: sp.GetRequiredService<TimeProvider>(),
            logger: sp.GetRequiredService<ILogger<PostHogClient>>()));
    }

    public void AddService<TInterface>(object service) => AddService(_ => service, typeof(TInterface), service.GetType());

    public void AddService<TInterface, TImplementation>(Func<IServiceProvider, object?> serviceFunc)
        => AddService(serviceFunc, typeof(TInterface), typeof(TImplementation));

    public void AddService(Func<IServiceProvider, object?> serviceFunc, Type interfaceType, Type implementationType)
    {
        _services[interfaceType] = serviceFunc;
        _services[implementationType] = serviceFunc;
    }

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var serviceFunc) ? serviceFunc(this) : null;
}