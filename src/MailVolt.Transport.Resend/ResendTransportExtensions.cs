using MailVolt.Transport.Resend;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// All MailVolt registration extensions live in MailVolt.Core.DependencyInjection, so one
// using covers AddMailVolt and every transport and template engine.
// ReSharper disable once CheckNamespace
namespace MailVolt.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering the Resend transport with the MailVolt pipeline.
/// </summary>
public static class ResendTransportExtensions
{
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    extension(MailVoltBuilder builder)
    {
        /// <summary>
        /// Registers the Resend email sender (<see cref="IResendSender"/> / <see cref="ISender"/>)
        /// as the active transport, binding <see cref="ResendSenderOptions"/> from the
        /// <c>"MailVolt:Resend"</c> configuration section.
        /// </summary>
        /// <returns>The same builder instance for chaining.</returns>
        public MailVoltBuilder UseResendTransport()
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddOptions<ResendSenderOptions>()
                .BindConfiguration(ResendSenderOptions.SectionName);

            AddResendSender(builder.Services);

            return builder;
        }

        /// <summary>
        /// Registers the Resend email sender (<see cref="IResendSender"/> / <see cref="ISender"/>)
        /// as the active transport, binding <see cref="ResendSenderOptions"/> from the
        /// <c>"MailVolt:Resend"</c> section of the supplied configuration.
        /// </summary>
        /// <param name="configuration">
        /// The configuration root — not a pre-scoped section. Every MailVolt transport takes
        /// the root and resolves its own section, so the section name lives in one place.
        /// </param>
        /// <returns>The same builder instance for chaining.</returns>
        public MailVoltBuilder UseResendTransport(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configuration);

            builder.Services.AddOptions<ResendSenderOptions>()
                .Bind(configuration.GetSection(ResendSenderOptions.SectionName));

            AddResendSender(builder.Services);

            return builder;
        }

        /// <summary>
        /// Registers the Resend email sender (<see cref="IResendSender"/> / <see cref="ISender"/>)
        /// as the active transport, configuring <see cref="ResendSenderOptions"/> via the specified
        /// delegate.
        /// </summary>
        /// <param name="configureOptions">A delegate to configure <see cref="ResendSenderOptions"/>.</param>
        /// <returns>The same builder instance for chaining.</returns>
        public MailVoltBuilder UseResendTransport(Action<ResendSenderOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configureOptions);

            builder.Services.AddOptions<ResendSenderOptions>()
                .Configure(configureOptions);

            AddResendSender(builder.Services);

            return builder;
        }
    }

    private static void AddResendSender(IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<global::Resend.ResendClientOptions>, ResendClientOptionsConfigure>();

        // Retries, a circuit breaker and a timeout, matching the Mailgun transport so the
        // documented resilience behaviour is actually true for every transport that owns
        // its own HttpClient.
        services.AddHttpClient<global::Resend.ResendClient>()
            .AddStandardResilienceHandler();
        services.AddTransient<global::Resend.IResend, global::Resend.ResendClient>();

        // Register the sender as both IResendSender and ISender
        services.AddTransient<IResendSender, ResendSender>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IResendSender>());
    }
}
