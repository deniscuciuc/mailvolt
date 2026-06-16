using MailVolt.Core.Interfaces;
using MailVolt.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.Core.DependencyInjection;

/// <summary>
/// Provides extension methods for registering MailVolt services with the DI container.
/// </summary>
public static class MailVoltServiceCollectionExtensions
{
    /// <summary>
    /// Registers MailVolt services with the specified configuration delegate.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An optional delegate to configure <see cref="MailVoltOptions"/>.</param>
    /// <returns>A <see cref="MailVoltBuilder"/> that can be used to further configure MailVolt.</returns>
    public static MailVoltBuilder AddMailVolt(
        this IServiceCollection services,
        Action<MailVoltOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<MailVoltOptions>(_ => { });
        }

        services.AddTransient<IEmailBuilder, EmailBuilder>();
        services.AddTransient<IBatchEmailSender, BatchEmailSender>();

        return new MailVoltBuilder(services);
    }

    /// <summary>
    /// Registers MailVolt services, binding <see cref="MailVoltOptions"/> from the specified configuration section.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration root. The <c>"MailVolt"</c> section will be bound to <see cref="MailVoltOptions"/>.</param>
    /// <returns>A <see cref="MailVoltBuilder"/> that can be used to further configure MailVolt.</returns>
    public static MailVoltBuilder AddMailVolt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind configuration manually to avoid AOT/trimming warnings with Configure<T>(IConfiguration)
        var section = configuration.GetSection(MailVoltOptions.SectionName);
        services.Configure<MailVoltOptions>(options =>
        {
            options.DefaultFromAddress = section[nameof(MailVoltOptions.DefaultFromAddress)];
            options.DefaultFromDisplayName = section[nameof(MailVoltOptions.DefaultFromDisplayName)];
        });

        services.AddTransient<IEmailBuilder, EmailBuilder>();
        services.AddTransient<IBatchEmailSender, BatchEmailSender>();

        return new MailVoltBuilder(services);
    }
}

/// <summary>
/// Builder class for chaining additional MailVolt configuration after calling <c>AddMailVolt</c>.
/// </summary>
public sealed class MailVoltBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MailVoltBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to which MailVolt services have been added.</param>
    public MailVoltBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> for further registration.
    /// </summary>
    public IServiceCollection Services { get; }
}
