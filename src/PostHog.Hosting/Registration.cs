using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PostHog;

public static class Registration
{
    /// <summary>
    /// Registers <see cref="PostHogClient"/> as a singleton. Looks for client configuration in the "PostHog"
    /// section of the configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <returns>The passed in <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">If <see cref="builder"/> is null.</exception>
    public static IHostApplicationBuilder AddPostHog(this IHostApplicationBuilder builder)
        => builder.AddPostHog(
            (builder ?? throw new ArgumentNullException(nameof(builder)) ).Configuration.GetSection("PostHog"));

    /// <summary>
    /// Registers <see cref="PostHogClient"/> as a singleton. Looks for client configuration in the supplied
    /// <paramref name="configurationSection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="configurationSection">The configuration section where to load config settings.</param>
    /// <returns>The passed in <see cref="IHostApplicationBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">If <see cref="builder"/> is null.</exception>
    public static IHostApplicationBuilder AddPostHog(
        this IHostApplicationBuilder builder,
        IConfigurationSection configurationSection)
    {
        builder = builder ?? throw new ArgumentNullException(nameof(builder));

        builder.Services.Configure<PostHogOptions>(configurationSection);
        builder.Services.AddSingleton<IPostHogClient, PostHogHostedClient>();
        return builder;
    }
}