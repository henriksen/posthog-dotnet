using Microsoft.Extensions.Configuration;
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
/// A container for dependency injection in unit tests. Provides some commonly used fakes.
/// </summary>
public sealed class TestContainer : IServiceProvider
{
    readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a <see cref="TestContainer"/> instance with the registered services.
    /// </summary>
    /// <param name="configureServices"></param>
    public TestContainer(Action<IServiceCollection>? configureServices = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        ConfigureServices(services);
        configureServices?.Invoke(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public FakeHttpMessageHandler FakeHttpMessageHandler { get; } = new();

    public FakeTimeProvider FakeTimeProvider { get; } = new();

    public FakeTaskScheduler FakeTaskScheduler { get; } = new();

    void ConfigureServices(IServiceCollection services)
    {
        services.Configure<PostHogOptions>(options =>
        {
            options.ProjectApiKey = "fake-project-api-key";
        });
        services.AddSingleton<FakeLoggerProvider>();
        services.AddLogging();
        services.AddSingleton<ILogger>(s => NullLogger.Instance);
        services.AddSingleton<ILoggerFactory>(s => s.GetRequiredService<FakeLoggerProvider>());
        services.AddSingleton<ILoggerProvider>(s => s.GetRequiredService<FakeLoggerProvider>());
        services.AddSingleton<HttpMessageHandler>(FakeHttpMessageHandler);
        services.AddSingleton<TimeProvider>(_ =>
        {
            FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
            return FakeTimeProvider;
        });
        services.AddSingleton<ITaskScheduler>(FakeTaskScheduler);
        services.AddSingleton<HttpClient>(_ => new HttpClient(FakeHttpMessageHandler));
        services.AddSingleton<IFeatureFlagCache>(NullFeatureFlagCache.Instance);
        services.AddSingleton<IPostHogApiClient, PostHogApiClient>(_ => CreatePostHogApiClient());
        services.AddSingleton<IPostHogClient, PostHogClient>();
    }

    PostHogApiClient CreatePostHogApiClient()
    {
        var httpClient = this.GetRequiredService<HttpClient>();
        var options = this.GetRequiredService<IOptions<PostHogOptions>>();
        return new PostHogApiClient(
            httpClient,
            options,
            FakeTimeProvider,
            logger: this.GetRequiredService<ILogger<PostHogApiClient>>());
    }

    /// <summary>
    /// Activates a new instance of <typeparamref name="T" /> using the services registered in the container.
    /// Use this for unit test subjects.
    /// </summary>
    /// <typeparam name="T">The type to activate.</typeparam>
    public T Activate<T>(params object[] parameters) where T : class
    {
        // Kludge: Temporary workaround.
        if (typeof(T) == typeof(PostHogApiClient))
        {
            return CreatePostHogApiClient() as T
                   ?? throw new InvalidOperationException("Could not create a PostHogApiClient.");
        }

        return ActivatorUtilities.CreateInstance<T>(_serviceProvider, parameters);
    }

    object? IServiceProvider.GetService(Type serviceType) => _serviceProvider.GetService(serviceType);
}