using AwesomeAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MailVolt.AutoConfigure.Tests;

/// <summary>
/// AutoConfigure covered only the InMemory and Smtp branches of WireTransport. Every
/// transport and template engine is now exercised, plus the missing-section and
/// unsupported-value error paths.
/// </summary>
public sealed class AllTransportsTests
{
    /// <summary>The transport name, and the minimum config its section needs to resolve.</summary>
    public static TheoryData<string, Dictionary<string, string?>> Transports => new()
    {
        { "Smtp", new() { ["MailVolt:Smtp:Host"] = "smtp.example.com" } },
        { "SendGrid", new() { ["MailVolt:SendGrid:ApiKey"] = "sg-key" } },
        {
            "Mailgun", new()
            {
                ["MailVolt:Mailgun:ApiKey"] = "mg-key",
                ["MailVolt:Mailgun:Domain"] = "mg.example.com",
            }
        },
        { "Resend", new() { ["MailVolt:Resend:ApiKey"] = "re-key" } },
        { "Postmark", new() { ["MailVolt:Postmark:ApiKey"] = "pm-token" } },
        {
            "Azure", new()
            {
                ["MailVolt:Azure:ConnectionString"] =
                    "endpoint=https://example.communication.azure.com/;accesskey=bm90LWEtcmVhbC1rZXk=",
            }
        },
        { "Brevo", new() { ["MailVolt:Brevo:ApiKey"] = "bv-key" } },
        {
            "AwsSes", new()
            {
                ["MailVolt:AwsSes:AccessKeyId"] = "AKIAEXAMPLE",
                ["MailVolt:AwsSes:SecretAccessKey"] = "secret",
                ["MailVolt:AwsSes:Region"] = "us-east-1",
            }
        },
        { "InMemory", new() },
    };

    [Theory]
    [MemberData(nameof(Transports))]
    public void Each_transport_resolves_an_ISender(string transport, Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddMailVolt(Config(transport, settings));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISender>().Should().NotBeNull();
        provider.GetRequiredService<IEmailBuilder>().Should().NotBeNull();
        provider.GetRequiredService<IBatchEmailSender>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("Smtp")]
    [InlineData("SendGrid")]
    [InlineData("Mailgun")]
    [InlineData("Resend")]
    [InlineData("Postmark")]
    [InlineData("Azure")]
    [InlineData("Brevo")]
    [InlineData("AwsSes")]
    public void A_transport_with_no_matching_section_fails_with_a_message_naming_the_section(string transport)
    {
        var services = new ServiceCollection();

        var act = () => services.AddMailVolt(Config(transport, []));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*MailVolt:{transport}*");
    }

    [Fact]
    public void InMemory_needs_no_section_of_its_own()
    {
        var services = new ServiceCollection();
        services.AddMailVolt(Config("InMemory", []));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISender>().Should().BeOfType<InMemorySender>();
    }

    [Theory]
    [InlineData("Razor")]
    [InlineData("Liquid")]
    [InlineData("Handlebars")]
    public void Each_template_engine_resolves_an_ITemplateRenderer(string engine)
    {
        var services = new ServiceCollection();
        services.AddMailVolt(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:From:Address"] = "noreply@example.com",
                ["MailVolt:Transport"] = "InMemory",
                ["MailVolt:Templates"] = engine,
            })
            .Build());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ITemplateRenderer>().Should().NotBeNull();
    }

    [Fact]
    public void No_template_engine_leaves_ITemplateRenderer_unregistered()
    {
        var services = new ServiceCollection();
        services.AddMailVolt(Config("InMemory", []));

        using var provider = services.BuildServiceProvider();

        provider.GetService<ITemplateRenderer>().Should().BeNull();
    }

    [Fact]
    public void An_unknown_transport_fails_rather_than_binding_nothing()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMailVolt(Config("Carrier Pigeon", []));

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void A_missing_From_address_fails_with_a_clear_message()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Transport"] = "InMemory",
            })
            .Build();

        var act = () => services.AddMailVolt(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*From*");
    }

    [Fact]
    public void A_missing_MailVolt_section_fails_with_a_clear_message()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        var act = () => services.AddMailVolt(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*MailVolt*not found*");
    }

    [Fact]
    public void A_custom_section_name_is_honoured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:From:Address"] = "noreply@example.com",
                ["Email:Transport"] = "InMemory",
            })
            .Build();

        services.AddMailVolt(configuration, "Email");

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ISender>().Should().BeOfType<InMemorySender>();
    }

    private static IConfigurationRoot Config(string transport, Dictionary<string, string?> settings)
    {
        var values = new Dictionary<string, string?>(settings)
        {
            ["MailVolt:From:Address"] = "noreply@example.com",
            ["MailVolt:From:DisplayName"] = "Example",
            ["MailVolt:Transport"] = transport,
        };

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
