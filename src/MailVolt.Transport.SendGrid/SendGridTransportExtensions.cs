using MailVolt.Core.Interfaces;
using MailVolt.Core.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.Transport.SendGrid.DependencyInjection;

/// <summary>
/// Extension methods for registering the SendGrid transport with the MailVolt builder.
/// </summary>
public static class SendGridTransportExtensions
{
    /// <summary>
    /// Registers the SendGrid sender using the specified configuration delegate.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> instance.</param>
    /// <param name="configureOptions">An optional delegate to configure <see cref="SendGridSenderOptions"/>.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder AddSendGridSender(
        this MailVoltBuilder builder,
        Action<SendGridSenderOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }
        else
        {
            builder.Services.Configure<SendGridSenderOptions>(_ => { });
        }

        RegisterSendGridSender(builder.Services);
        return builder;
    }

    /// <summary>
    /// Registers the SendGrid sender, binding <see cref="SendGridSenderOptions"/> from the
    /// <c>"MailVolt:SendGrid"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> instance.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder AddSendGridSender(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<SendGridSenderOptions>(
            configuration.GetSection(SendGridSenderOptions.SectionName));

        RegisterSendGridSender(builder.Services);
        return builder;
    }

    private static void RegisterSendGridSender(IServiceCollection services)
    {
        services.AddHttpClient<ISendGridSender, SendGridSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SendGridSenderOptions>>();
            client.BaseAddress ??= new Uri(options.Value.BaseUrl);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        });

        services.AddTransient<ISender>(sp => sp.GetRequiredService<ISendGridSender>());
    }
}
