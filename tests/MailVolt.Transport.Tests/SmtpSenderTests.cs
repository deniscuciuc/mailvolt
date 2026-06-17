using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Smtp;
using MailVolt.Transport.Smtp.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.Transport.Tests;

public sealed class SmtpSenderTests
{
    [Fact]
    public void SmtpSender_implements_ISender()
    {
        typeof(SmtpSender).Should().Implement<ISender>();
    }

    [Fact]
    public void SmtpSender_is_created_with_IOptions()
    {
        var options = Substitute.For<IOptions<SmtpSenderOptions>>();
        options.Value.Returns(new SmtpSenderOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            Username = "user",
            Password = "pass"
        });

        var sender = new SmtpSender(options);

        sender.Should().NotBeNull();
        sender.Should().BeAssignableTo<ISender>();
    }

    [Fact]
    public void UseSmtpTransport_with_delegate_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        builder.UseSmtpTransport(opts =>
        {
            opts.Host = "smtp.example.com";
            opts.Port = 587;
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<SmtpSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<SmtpSenderOptions>>().Value;
        resolvedOptions.Host.Should().Be("smtp.example.com");
        resolvedOptions.Port.Should().Be(587);
    }

    [Fact]
    public void UseSmtpTransport_with_configuration_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Smtp:Host"] = "mail.example.com",
                ["MailVolt:Smtp:Port"] = "465"
            })
            .Build();

        builder.UseSmtpTransport(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<SmtpSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<SmtpSenderOptions>>().Value;
        resolvedOptions.Host.Should().Be("mail.example.com");
        resolvedOptions.Port.Should().Be(465);
    }
}
