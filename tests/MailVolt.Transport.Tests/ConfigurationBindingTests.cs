using AwesomeAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Transport.AwsSes;
using MailVolt.Transport.AzureEmail;
using MailVolt.Transport.Brevo;
using MailVolt.Transport.Mailgun;
using MailVolt.Transport.Postmark;
using MailVolt.Transport.Resend;
using MailVolt.Transport.SendGrid;
using MailVolt.Transport.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Every transport's IConfiguration overload takes the configuration <em>root</em> and
/// resolves its own <c>MailVolt:X</c> section. Passing a pre-scoped section instead resolves
/// to <c>MailVolt:X:MailVolt:X</c> and silently yields empty options — which is what eight
/// docs pages used to tell people to do. These tests pin the convention.
/// </summary>
public sealed class ConfigurationBindingTests
{
    [Fact]
    public void Smtp_binds_from_the_configuration_root()
    {
        var options = Bind<SmtpSenderOptions>(
            new() { ["MailVolt:Smtp:Host"] = "smtp.example.com", ["MailVolt:Smtp:Port"] = "2525" },
            (builder, configuration) => builder.UseSmtpTransport(configuration));

        options.Host.Should().Be("smtp.example.com");
        options.Port.Should().Be(2525);
    }

    [Fact]
    public void SendGrid_binds_from_the_configuration_root()
    {
        var options = Bind<SendGridSenderOptions>(
            new() { ["MailVolt:SendGrid:ApiKey"] = "sg-key" },
            (builder, configuration) => builder.UseSendGridTransport(configuration));

        options.ApiKey.Should().Be("sg-key");
    }

    [Fact]
    public void Mailgun_binds_from_the_configuration_root()
    {
        var options = Bind<MailgunSenderOptions>(
            new()
            {
                ["MailVolt:Mailgun:ApiKey"] = "mg-key",
                ["MailVolt:Mailgun:Domain"] = "mg.example.com",
            },
            (builder, configuration) => builder.UseMailgunTransport(configuration));

        options.ApiKey.Should().Be("mg-key");
        options.Domain.Should().Be("mg.example.com");
    }

    [Fact]
    public void Resend_binds_from_the_configuration_root()
    {
        // Resend previously took a pre-scoped IConfigurationSection, so it was the one
        // transport where the docs happened to be right and the convention was wrong.
        var options = Bind<ResendSenderOptions>(
            new() { ["MailVolt:Resend:ApiKey"] = "re-key" },
            (builder, configuration) => builder.UseResendTransport(configuration));

        options.ApiKey.Should().Be("re-key");
    }

    [Fact]
    public void Postmark_binds_from_the_configuration_root()
    {
        var options = Bind<PostmarkSenderOptions>(
            new() { ["MailVolt:Postmark:ApiKey"] = "pm-key" },
            (builder, configuration) => builder.UsePostmarkTransport(configuration));

        options.ApiKey.Should().Be("pm-key");
    }

    [Fact]
    public void Azure_binds_from_the_configuration_root()
    {
        var options = Bind<AzureEmailSenderOptions>(
            new() { ["MailVolt:Azure:ConnectionString"] = "endpoint=https://x/;accesskey=k" },
            (builder, configuration) => builder.UseAzureEmailTransport(configuration));

        options.ConnectionString.Should().Be("endpoint=https://x/;accesskey=k");
    }

    [Fact]
    public void Brevo_binds_from_the_configuration_root()
    {
        var options = Bind<BrevoSenderOptions>(
            new() { ["MailVolt:Brevo:ApiKey"] = "bv-key" },
            (builder, configuration) => builder.UseBrevoTransport(configuration));

        options.ApiKey.Should().Be("bv-key");
    }

    [Fact]
    public void AwsSes_binds_from_the_configuration_root()
    {
        var options = Bind<AwsSesSenderOptions>(
            new()
            {
                ["MailVolt:AwsSes:AccessKeyId"] = "AKIAEXAMPLE",
                ["MailVolt:AwsSes:SecretAccessKey"] = "secret",
                ["MailVolt:AwsSes:Region"] = "eu-west-1",
            },
            (builder, configuration) => builder.UseAwsSesTransport(configuration));

        options.AccessKeyId.Should().Be("AKIAEXAMPLE");
        options.Region.Should().Be("eu-west-1");
    }

    [Fact]
    public void Passing_a_pre_scoped_section_yields_empty_options()
    {
        // Documents the failure mode explicitly: GetSection re-scopes, so the values end up
        // under MailVolt:Smtp:MailVolt:Smtp and nothing binds.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Smtp:Host"] = "smtp.example.com",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMailVolt().UseSmtpTransport(configuration.GetSection("MailVolt:Smtp"));

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOptions<SmtpSenderOptions>>().Value.Host.Should().BeNull();
    }

    private static TOptions Bind<TOptions>(
        Dictionary<string, string?> settings,
        Action<MailVoltBuilder, IConfiguration> register)
        where TOptions : class
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        // BindConfiguration needs IConfiguration in the container.
        services.AddSingleton<IConfiguration>(configuration);
        register(services.AddMailVolt(), configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<TOptions>>().Value;
    }
}
