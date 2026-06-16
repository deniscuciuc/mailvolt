using MailVolt.Core.Interfaces;
using MailVolt.Core.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace MailVolt.Transport.Brevo.DependencyInjection;

/// <summary>
/// Extension methods for registering the Brevo email transport.
/// </summary>
public static class BrevoTransportExtensions
{
    /// <summary>
    /// Registers the Brevo sender as the <c>ISender</c> implementation, binding
    /// <see cref="BrevoSenderOptions"/> from the <c>"MailVolt:Brevo"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> from <c>AddMailVolt</c>.</param>
    /// <param name="configuration">The configuration root. The <c>"MailVolt:Brevo"</c> section will be bound.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder AddBrevoSender(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<BrevoSenderOptions>(
            configuration.GetSection(BrevoSenderOptions.SectionName));

        RegisterServices(builder);

        return builder;
    }

    /// <summary>
    /// Registers the Brevo sender as the <c>ISender</c> implementation, configuring
    /// <see cref="BrevoSenderOptions"/> via a delegate.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> from <c>AddMailVolt</c>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="BrevoSenderOptions"/>.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder AddBrevoSender(
        this MailVoltBuilder builder,
        Action<BrevoSenderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services.Configure(configureOptions);
        RegisterServices(builder);

        return builder;
    }

    private static void RegisterServices(MailVoltBuilder builder)
    {
        builder.Services
            .AddHttpClient<IBrevoSender, BrevoSender>(client =>
            {
                client.BaseAddress = new Uri("https://api.brevo.com");
            })
            .AddStandardResilienceHandler();

        builder.Services.AddTransient<ISender>(sp => sp.GetRequiredService<IBrevoSender>());
    }
}
