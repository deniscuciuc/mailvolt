using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MailVolt.Transport.Resend.DependencyInjection;

/// <summary>
/// Extension methods for registering the Resend transport with the MailVolt pipeline.
/// </summary>
public static class ResendTransportExtensions
{
    /// <summary>
    /// Registers the Resend email sender (<see cref="IResendSender"/> / <see cref="ISender"/>)
    /// as the active transport, binding <see cref="ResendSenderOptions"/> from the
    /// <c>"MailVolt:Resend"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static MailVoltBuilder UseResend(this MailVoltBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<ResendSenderOptions>()
            .BindConfiguration(ResendSenderOptions.SectionName);

        AddResendSender(builder.Services);

        return builder;
    }

    /// <summary>
    /// Registers the Resend email sender (<see cref="IResendSender"/> / <see cref="ISender"/>)
    /// as the active transport, binding <see cref="ResendSenderOptions"/> from the specified
    /// configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <param name="configurationSection">The configuration section to bind options from.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static MailVoltBuilder UseResend(
        this MailVoltBuilder builder,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationSection);

        builder.Services.AddOptions<ResendSenderOptions>()
            .Bind(configurationSection);

        AddResendSender(builder.Services);

        return builder;
    }

    /// <summary>
    /// Registers the Resend email sender (<see cref="IResendSender"/> / <see cref="ISender"/>)
    /// as the active transport, configuring <see cref="ResendSenderOptions"/> via the specified
    /// delegate.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="ResendSenderOptions"/>.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static MailVoltBuilder UseResend(
        this MailVoltBuilder builder,
        Action<ResendSenderOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services.AddOptions<ResendSenderOptions>()
            .Configure(configureOptions);

        AddResendSender(builder.Services);

        return builder;
    }

    private static void AddResendSender(IServiceCollection services)
    {
        services.AddHttpClient<IResendSender, ResendSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ResendSenderOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
        })
        .AddStandardResilienceHandler();

        // Also register as the default ISender so BatchEmailSender picks it up
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IResendSender>());
    }
}
