namespace MailVolt.Transport.Mailgun.DependencyInjection;

using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the Mailgun transport with the MailVolt pipeline.
/// </summary>
public static class MailgunTransportExtensions
{
    /// <summary>
    /// Registers the Mailgun transport using an inline configuration delegate.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> returned by <c>AddMailVolt</c>.</param>
    /// <param name="configure">A delegate to configure <see cref="MailgunSenderOptions"/>.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static MailVoltBuilder UseMailgunTransport(
        this MailVoltBuilder builder,
        Action<MailgunSenderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(configure);
        builder.Services
            .AddHttpClient<IMailgunSender, MailgunSender>()
            .AddStandardResilienceHandler();
        builder.Services.AddTransient<ISender>(sp => sp.GetRequiredService<IMailgunSender>());
        return builder;
    }

    /// <summary>
    /// Registers the Mailgun transport, binding <see cref="MailgunSenderOptions"/>
    /// from the <c>"MailVolt:Mailgun"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> returned by <c>AddMailVolt</c>.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static MailVoltBuilder UseMailgunTransport(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<MailgunSenderOptions>(
            configuration.GetSection(MailgunSenderOptions.SectionName));
        builder.Services
            .AddHttpClient<IMailgunSender, MailgunSender>()
            .AddStandardResilienceHandler();
        builder.Services.AddTransient<ISender>(sp => sp.GetRequiredService<IMailgunSender>());
        return builder;
    }
}
