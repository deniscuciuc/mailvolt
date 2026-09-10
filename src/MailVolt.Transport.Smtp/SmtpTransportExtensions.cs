
using MailVolt.Transport.Smtp;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// All MailVolt registration extensions live in MailVolt.Core.DependencyInjection, so one
// using covers AddMailVolt and every transport and template engine.
// ReSharper disable once CheckNamespace
namespace MailVolt.Core.DependencyInjection;

public static class SmtpTransportExtensions
{
    extension(MailVoltBuilder builder)
    {
        public MailVoltBuilder UseSmtpTransport(Action<SmtpSenderOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configure);

            builder.Services.Configure(configure);
            builder.Services.AddTransient<ISender, SmtpSender>();
            return builder;
        }

        public MailVoltBuilder UseSmtpTransport(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configuration);

            builder.Services.Configure<SmtpSenderOptions>(
                configuration.GetSection(SmtpSenderOptions.SectionName));
            builder.Services.AddTransient<ISender, SmtpSender>();
            return builder;
        }
    }
}
