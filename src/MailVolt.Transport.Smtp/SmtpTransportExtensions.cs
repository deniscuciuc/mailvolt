namespace MailVolt.Transport.Smtp.DependencyInjection;

using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class SmtpTransportExtensions
{
    public static MailVoltBuilder UseSmtpTransport(
        this MailVoltBuilder builder,
        Action<SmtpSenderOptions> configure)
    {
        builder.Services.Configure(configure);
        builder.Services.AddTransient<ISender, SmtpSender>();
        return builder;
    }

    public static MailVoltBuilder UseSmtpTransport(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        builder.Services.Configure<SmtpSenderOptions>(
            configuration.GetSection(SmtpSenderOptions.SectionName));
        builder.Services.AddTransient<ISender, SmtpSender>();
        return builder;
    }
}
