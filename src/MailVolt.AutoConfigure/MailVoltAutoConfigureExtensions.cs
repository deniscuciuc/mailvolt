using MailVolt.Core.DependencyInjection;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Templates.Handlebars.DependencyInjection;
using MailVolt.Templates.Liquid.DependencyInjection;
using MailVolt.Templates.Razor.DependencyInjection;
using MailVolt.Transport.AwsSes.DependencyInjection;
using MailVolt.Transport.AzureEmail.DependencyInjection;
using MailVolt.Transport.Brevo.DependencyInjection;
using MailVolt.Transport.Mailgun.DependencyInjection;
using MailVolt.Transport.Resend.DependencyInjection;
using MailVolt.Transport.SendGrid.DependencyInjection;
using MailVolt.Transport.Smtp.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.AutoConfigure;

/// <summary>
/// Extension methods for zero-code configuration of MailVolt via <see cref="IConfiguration"/>.
/// </summary>
public static class MailVoltAutoConfigureExtensions
{
    /// <summary>
    /// Registers MailVolt services by reading the <c>MailVolt</c> configuration section.
    /// Automatically wires the selected transport and optional template engine.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="sectionName">The configuration section name (default <c>"MailVolt"</c>).</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMailVolt(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "MailVolt")
    {
        var section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"Configuration section '{sectionName}' not found.");
        }

        var opts = new MailVoltAutoOptions();
        section.Bind(opts);

        if (string.IsNullOrWhiteSpace(opts.From.Address))
        {
            throw new InvalidOperationException(
                "'MailVolt:From:Address' must be configured.");
        }

        var builder = services.AddMailVolt(o =>
        {
            o.DefaultFromAddress = opts.From.Address;
            o.DefaultFromDisplayName = opts.From.DisplayName;
        });

        WireTransport(builder, opts.Transport, section);

        if (opts.Templates.HasValue)
        {
            WireTemplateEngine(builder, opts.Templates.Value);
        }

        return services;
    }

    private static void WireTransport(
        MailVoltBuilder builder,
        MailVoltTransport transport,
        IConfigurationSection section)
    {
        switch (transport)
        {
            case MailVoltTransport.Smtp:
                RequireSection(section, "Smtp");
                builder.UseSmtpTransport(o => section.GetSection("Smtp").Bind(o));
                break;
            case MailVoltTransport.SendGrid:
                RequireSection(section, "SendGrid");
                builder.AddSendGridSender(o => section.GetSection("SendGrid").Bind(o));
                break;
            case MailVoltTransport.Mailgun:
                RequireSection(section, "Mailgun");
                builder.UseMailgunTransport(o => section.GetSection("Mailgun").Bind(o));
                break;
            case MailVoltTransport.Resend:
                RequireSection(section, "Resend");
                builder.UseResend(o => section.GetSection("Resend").Bind(o));
                break;
            case MailVoltTransport.Postmark:
                RequireSection(section, "Postmark");
                builder.AddPostmarkSender(o => section.GetSection("Postmark").Bind(o));
                break;
            case MailVoltTransport.Azure:
                RequireSection(section, "Azure");
                builder.AddAzureEmailSender(o => section.GetSection("Azure").Bind(o));
                break;
            case MailVoltTransport.Brevo:
                RequireSection(section, "Brevo");
                builder.AddBrevoSender(o => section.GetSection("Brevo").Bind(o));
                break;
            case MailVoltTransport.AwsSes:
                RequireSection(section, "AwsSes");
                builder.UseAwsSesTransport(o => section.GetSection("AwsSes").Bind(o));
                break;
            case MailVoltTransport.InMemory:
                builder.UseInMemoryTransport();
                break;
            default:
                throw new NotSupportedException(
                    $"Transport '{transport}' is not supported.");
        }
    }

    private static void RequireSection(IConfigurationSection parent, string key)
    {
        var section = parent.GetSection(key);
        if (!section.Exists())
        {
            throw new InvalidOperationException(
                $"'{parent.Path}:Transport' is set to '{key}' " +
                $"but '{parent.Path}:{key}' section is missing from configuration. " +
                $"Add a '{parent.Path}:{key}' section with the required credentials.");
        }
    }

    private static void WireTemplateEngine(
        MailVoltBuilder builder,
        MailVoltTemplateEngine engine)
    {
        switch (engine)
        {
            case MailVoltTemplateEngine.Razor:
                builder.UseRazorTemplates();
                break;
            case MailVoltTemplateEngine.Liquid:
                builder.UseLiquidTemplates();
                break;
            case MailVoltTemplateEngine.Handlebars:
                builder.UseHandlebarsTemplates();
                break;
            default:
                throw new NotSupportedException(
                    $"Template engine '{engine}' is not supported.");
        }
    }
}
