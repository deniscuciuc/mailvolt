using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SendGrid.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace MailVolt.Transport.SendGrid.DependencyInjection;

/// <summary>
/// Extension methods for registering the SendGrid transport with the MailVolt builder.
/// </summary>
public static class SendGridTransportExtensions
{
    /// <param name="builder">The <see cref="MailVoltBuilder"/> instance.</param>
    extension(MailVoltBuilder builder)
    {
        /// <summary>
        /// Registers the SendGrid sender using the specified configuration delegate.
        /// </summary>
        /// <param name="configureOptions">An optional delegate to configure <see cref="SendGridSenderOptions"/>.</param>
        /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
        public MailVoltBuilder AddSendGridSender( // ReSharper disable once MethodOverloadWithOptionalParameter
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
        /// <param name="configuration">The configuration root.</param>
        /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
        public MailVoltBuilder AddSendGridSender(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configuration);

            builder.Services.Configure<SendGridSenderOptions>(
                configuration.GetSection(SendGridSenderOptions.SectionName));

            RegisterSendGridSender(builder.Services);
            return builder;
        }

        /// <summary>
        /// Registers the SendGrid sender without additional configuration.
        /// Useful when <see cref="SendGridSenderOptions"/> are configured elsewhere.
        /// </summary>
        /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
        public MailVoltBuilder AddSendGridSender()
        {
            ArgumentNullException.ThrowIfNull(builder);

            RegisterSendGridSender(builder.Services);
            return builder;
        }
    }

    private static void RegisterSendGridSender(IServiceCollection services)
    {
        services.AddSendGrid((serviceProvider, options) =>
        {
            var senderOptions = serviceProvider.GetRequiredService<IOptions<SendGridSenderOptions>>().Value;
            options.ApiKey = senderOptions.ApiKey;
            options.Host = senderOptions.BaseUrl.TrimEnd('/');
        });

        // Register the sender as both ISendGridSender and ISender
        services.AddTransient<ISendGridSender, SendGridSender>();
        services.AddTransient<ISender>(serviceProvider => serviceProvider.GetRequiredService<ISendGridSender>());
    }
}
