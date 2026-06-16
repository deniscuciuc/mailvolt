using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Smtp.DependencyInjection;
using MailVolt.Transport.Mailgun.DependencyInjection;
using MailVolt.Transport.SendGrid.DependencyInjection;
using MailVolt.Transport.Resend.DependencyInjection;
using MailVolt.Transport.AzureEmail.DependencyInjection;
using MailVolt.Transport.Brevo.DependencyInjection;
using MailVolt.Transport.AwsSes.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Verifies that each transport's DI extension correctly registers <see cref="ISender"/>.
/// </summary>
public sealed class TransportDiTests
{
    [Fact]
    public void Smtp_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = services.AddMailVolt();

        builder.UseSmtpTransport(opts =>
        {
            opts.Host = "smtp.example.com";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void Mailgun_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = services.AddMailVolt();

        builder.UseMailgunTransport(opts =>
        {
            opts.ApiKey = "mg-key";
            opts.Domain = "mg.example.com";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void SendGrid_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddMailVolt();

        builder.AddSendGridSender(opts =>
        {
            opts.ApiKey = "sg-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void Resend_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddMailVolt();

        builder.UseResend(opts =>
        {
            opts.ApiKey = "resend-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void Postmark_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = services.AddMailVolt();

        builder.AddPostmarkSender(opts =>
        {
            opts.ApiKey = "pm-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void AzureEmail_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = services.AddMailVolt();

        builder.AddAzureEmailSender(opts =>
        {
            opts.ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA==";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void Brevo_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = services.AddMailVolt();

        builder.AddBrevoSender(opts =>
        {
            opts.ApiKey = "brevo-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void AwsSes_transport_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = services.AddMailVolt();

        builder.UseAwsSesTransport(opts =>
        {
            opts.AccessKeyId = "AKIA123";
            opts.SecretAccessKey = "secret456";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Fact]
    public void Each_transport_extension_accepts_configuration_section()
    {
        // Verify that all configuration-based overloads compile and run
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Smtp:Host"] = "smtp.example.com",
                ["MailVolt:Mailgun:ApiKey"] = "mg-key",
                ["MailVolt:Mailgun:Domain"] = "mg.example.com",
                ["MailVolt:SendGrid:ApiKey"] = "sg-key",
                ["MailVolt:Azure:ConnectionString"] = "endpoint=https://test.communication.azure.com/;accesskey=Zm9v",
                ["MailVolt:Brevo:ApiKey"] = "brevo-key",
                ["MailVolt:AwsSes:AccessKeyId"] = "AKIA123",
                ["MailVolt:AwsSes:SecretAccessKey"] = "secret456"
            })
            .Build();

        // Smtp with config
        var smtpServices = new ServiceCollection();
        smtpServices.AddMailVolt().UseSmtpTransport(config);
        smtpServices.BuildServiceProvider().GetRequiredService<ISender>().Should().NotBeNull();

        // Mailgun with config
        var mailgunServices = new ServiceCollection();
        mailgunServices.AddMailVolt().UseMailgunTransport(config);
        mailgunServices.BuildServiceProvider().GetRequiredService<ISender>().Should().NotBeNull();

        // SendGrid with config
        var sendGridServices = new ServiceCollection();
        sendGridServices.AddLogging();
        sendGridServices.AddMailVolt().AddSendGridSender(config);
        sendGridServices.BuildServiceProvider().GetRequiredService<ISender>().Should().NotBeNull();

        // Azure with config
        var azureServices = new ServiceCollection();
        azureServices.AddMailVolt().AddAzureEmailSender(config);
        azureServices.BuildServiceProvider().GetRequiredService<ISender>().Should().NotBeNull();

        // Brevo with config
        var brevoServices = new ServiceCollection();
        brevoServices.AddMailVolt().AddBrevoSender(config);
        brevoServices.BuildServiceProvider().GetRequiredService<ISender>().Should().NotBeNull();

        // AwsSes with config
        var awsServices = new ServiceCollection();
        awsServices.AddMailVolt().UseAwsSesTransport(config);
        awsServices.BuildServiceProvider().GetRequiredService<ISender>().Should().NotBeNull();
    }
}
