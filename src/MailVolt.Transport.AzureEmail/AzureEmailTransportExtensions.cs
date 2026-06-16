using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace MailVolt.Transport.AzureEmail.DependencyInjection;

/// <summary>
/// Extension methods for registering the Azure Email Communication Services transport.
/// </summary>
public static class AzureEmailTransportExtensions
{
    /// <summary>
    /// Registers the Azure Email sender as the <c>ISender</c> implementation, binding
    /// <see cref="AzureEmailSenderOptions"/> from the <c>"MailVolt:Azure"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> from <c>AddMailVolt</c>.</param>
    /// <param name="configuration">The configuration root. The <c>"MailVolt:Azure"</c> section will be bound.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder AddAzureEmailSender(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<AzureEmailSenderOptions>(
            configuration.GetSection(AzureEmailSenderOptions.SectionName));

        builder.Services.AddTransient<ISender, AzureEmailSender>();

        return builder;
    }

    /// <summary>
    /// Registers the Azure Email sender as the <c>ISender</c> implementation, configuring
    /// <see cref="AzureEmailSenderOptions"/> via a delegate.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> from <c>AddMailVolt</c>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="AzureEmailSenderOptions"/>.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder AddAzureEmailSender(
        this MailVoltBuilder builder,
        Action<AzureEmailSenderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services.Configure(configureOptions);
        builder.Services.AddTransient<ISender, AzureEmailSender>();

        return builder;
    }
}
