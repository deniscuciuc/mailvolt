using AwesomeAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Options;
using MailVolt.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.AutoConfigure.Tests;

public sealed class AutoConfigureTests
{
    [Fact]
    public void AddMailVolt_InMemoryTransport_RegistersInMemorySender()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "test@example.com",
                ["MailVolt:Transport"] = "InMemory",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMailVolt(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().BeOfType<InMemorySender>();
    }

    [Fact]
    public void AddMailVolt_SmtpTransport_RegistersSmtpSender()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "test@example.com",
                ["MailVolt:Transport"] = "Smtp",
                ["MailVolt:Smtp:Host"] = "smtp.example.com",
                ["MailVolt:Smtp:Port"] = "587",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMailVolt(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Razor", typeof(ITemplateRenderer))]
    [InlineData("Liquid", typeof(ITemplateRenderer))]
    [InlineData("Handlebars", typeof(ITemplateRenderer))]
    public void AddMailVolt_TemplateEngine_RegistersCorrectRenderer(
        string templateEngine, Type rendererInterface)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "test@example.com",
                ["MailVolt:Transport"] = "InMemory",
                ["MailVolt:Templates"] = templateEngine,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMailVolt(config);

        // Verify the service descriptor is present for ITemplateRenderer
        var descriptor = services.FirstOrDefault(s => s.ServiceType == rendererInterface);
        descriptor.Should().NotBeNull();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddMailVolt_NoTemplates_DoesNotRegisterRenderer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "test@example.com",
                ["MailVolt:Transport"] = "InMemory",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMailVolt(config);

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITemplateRenderer));
        descriptor.Should().BeNull();
    }

    [Fact]
    public void AddMailVolt_MissingSectionThrows()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        var act = () => services.AddMailVolt(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Configuration section 'MailVolt' not found.");
    }

    [Fact]
    public void AddMailVolt_MissingFromAddressThrows()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Transport"] = "InMemory",
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddMailVolt(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("'MailVolt:From:Address' must be configured.");
    }

    [Fact]
    public void AddMailVolt_TransportSmtp_MissingSmtpSectionThrows()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "test@example.com",
                ["MailVolt:Transport"] = "Smtp",
            })
            .Build();

        var services = new ServiceCollection();
        var act = () => services.AddMailVolt(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Smtp*section*missing*");
    }

    [Fact]
    public void AddMailVolt_CustomSectionName_Works()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MyEmail:From:Address"] = "custom@example.com",
                ["MyEmail:Transport"] = "InMemory",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMailVolt(config, "MyEmail");

        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        sender.Should().BeOfType<InMemorySender>();
    }

    [Fact]
    public void AddMailVolt_DefaultFromOptions_AreSetOnMailVoltOptions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "noreply@example.com",
                ["MailVolt:From:DisplayName"] = "My App",
                ["MailVolt:Transport"] = "InMemory",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMailVolt(config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<MailVoltOptions>>();

        options.Value.DefaultFromAddress.Should().Be("noreply@example.com");
        options.Value.DefaultFromDisplayName.Should().Be("My App");
    }
}
