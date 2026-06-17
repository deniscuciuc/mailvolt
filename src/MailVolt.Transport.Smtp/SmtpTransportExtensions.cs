
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace MailVolt.Transport.Smtp.DependencyInjection;

public static class SmtpTransportExtensions
{
    extension(MailVoltBuilder builder)
    {
        public MailVoltBuilder UseSmtpTransport(Action<SmtpSenderOptions> configure)
        {
            builder.Services.Configure(configure);
            builder.Services.AddTransient<ISender, SmtpSender>();
            return builder;
        }

        public MailVoltBuilder UseSmtpTransport(IConfiguration configuration)
        {
            builder.Services.Configure<SmtpSenderOptions>(
                configuration.GetSection(SmtpSenderOptions.SectionName));
            builder.Services.AddTransient<ISender, SmtpSender>();
            return builder;
        }
    }
}
