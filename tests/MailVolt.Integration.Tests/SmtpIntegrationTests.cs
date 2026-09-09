using AwesomeAssertions;
using MailKit.Security;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Smtp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MailVolt.Integration.Tests;

/// <summary>
/// Exercises <c>MailVolt.Transport.Smtp</c> against a real SMTP server. These cover the
/// wire format — multipart structure, inline images, headers — which the unit tests
/// cannot reach because they never serialize a message.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(MailpitCollection))]
public sealed class SmtpIntegrationTests(MailpitFixture mailpit) : IAsyncLifetime
{
    public Task InitializeAsync() => mailpit.ClearAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMailVolt()
            .UseSmtpTransport(options =>
            {
                options.Host = mailpit.Host;
                options.Port = mailpit.SmtpMappedPort;
                options.Security = SecureSocketOptions.None;
            });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_delivers_a_simple_email()
    {
        await using var provider = BuildProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        var result = await builder
            .From(new EmailAddress("sender@example.com", "MailVolt Sender"))
            .To(new EmailAddress("recipient@example.com", "A Recipient"))
            .Subject("Integration test")
            .HtmlBody("<h1>Hello from MailVolt!</h1>")
            .TextBody("Hello from MailVolt!")
            .SendAsync();

        result.IsSuccess.Should().BeTrue(result.Error);

        var messages = await mailpit.GetMessagesAsync();
        messages.Should().ContainSingle();

        var message = await mailpit.GetMessageAsync(messages[0].Id);
        message.Subject.Should().Be("Integration test");
        message.From!.Address.Should().Be("sender@example.com");
        message.From.Name.Should().Be("MailVolt Sender");
        message.To.Should().ContainSingle(x => x.Address == "recipient@example.com");
        message.Html.Should().Contain("Hello from MailVolt!");
        message.Text.Should().Contain("Hello from MailVolt!");
    }

    [Fact]
    public async Task SendAsync_delivers_cc_bcc_and_reply_to()
    {
        await using var provider = BuildProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        var result = await builder
            .From("sender@example.com")
            .To("to@example.com")
            .Cc("cc@example.com")
            .Bcc("bcc@example.com")
            .ReplyTo("reply@example.com")
            .Subject("Recipients")
            .TextBody("body")
            .SendAsync();

        result.IsSuccess.Should().BeTrue(result.Error);

        var messages = await mailpit.GetMessagesAsync();
        var message = await mailpit.GetMessageAsync(messages[0].Id);

        message.To.Should().ContainSingle(x => x.Address == "to@example.com");
        message.Cc.Should().ContainSingle(x => x.Address == "cc@example.com");
        message.ReplyTo.Should().ContainSingle(x => x.Address == "reply@example.com");

        // The Bcc recipient must receive the mail...
        message.Bcc.Should().ContainSingle(x => x.Address == "bcc@example.com");

        // ...but must not be disclosed in the transmitted headers. MailKit hides the
        // Bcc header on send; Mailpit reconstructs it from the SMTP envelope and
        // prepends it, so only the headers MailVolt itself wrote are checked here.
        var raw = await mailpit.GetRawAsync(messages[0].Id);
        var sentHeaders = raw[raw.IndexOf("From: sender@example.com", StringComparison.Ordinal)..];
        sentHeaders.Should().NotContain("Bcc:");
    }

    [Fact]
    public async Task SendAsync_delivers_an_attachment()
    {
        await using var provider = BuildProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        var result = await builder
            .From("sender@example.com")
            .To("recipient@example.com")
            .Subject("With attachment")
            .TextBody("see attached")
            .Attach(a => a
                .FromBytes("report.csv", "id,name\n1,alice\n"u8.ToArray())
                .WithContentType("text/csv"))
            .SendAsync();

        result.IsSuccess.Should().BeTrue(result.Error);

        var messages = await mailpit.GetMessagesAsync();
        var message = await mailpit.GetMessageAsync(messages[0].Id);

        message.Attachments.Should().ContainSingle();
        message.Attachments[0].FileName.Should().Be("report.csv");
        message.Attachments[0].ContentType.Should().Be("text/csv");
        message.Attachments[0].Size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SendAsync_delivers_an_inline_image_as_a_linked_resource()
    {
        await using var provider = BuildProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        // A 1x1 transparent PNG.
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

        var result = await builder
            .From("sender@example.com")
            .To("recipient@example.com")
            .Subject("Inline image")
            .HtmlBody("""<p>logo: <img src="cid:logo@mailvolt" /></p>""")
            .Attach(a => a.FromBytes("logo.png", png).AsInlineImage("logo@mailvolt"))
            .SendAsync();

        result.IsSuccess.Should().BeTrue(result.Error);

        var messages = await mailpit.GetMessagesAsync();
        var message = await mailpit.GetMessageAsync(messages[0].Id);

        // An inline image must be a linked resource, not a plain attachment, or mail
        // clients show it as a downloadable file instead of rendering it.
        message.Inline.Should().ContainSingle();
        message.Inline[0].ContentId.Should().Be("logo@mailvolt");
        message.Inline[0].ContentType.Should().Be("image/png");
        message.Attachments.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_delivers_priority_and_custom_headers()
    {
        await using var provider = BuildProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        var result = await builder
            .From("sender@example.com")
            .To("recipient@example.com")
            .Subject("Headers")
            .TextBody("body")
            .Priority(EmailPriority.High)
            .Header("X-Campaign-Id", "welcome-2026")
            .SendAsync();

        result.IsSuccess.Should().BeTrue(result.Error);

        var messages = await mailpit.GetMessagesAsync();
        var raw = await mailpit.GetRawAsync(messages[0].Id);

        raw.Should().Contain("X-Priority: 1");
        raw.Should().Contain("X-Campaign-Id: welcome-2026");
    }

    [Fact]
    public async Task SendAsync_returns_a_failure_when_the_server_is_unreachable()
    {
        var services = new ServiceCollection();
        services.AddMailVolt()
            .UseSmtpTransport(options =>
            {
                options.Host = "127.0.0.1";
                // Nothing listens here.
                options.Port = 1;
                options.Security = SecureSocketOptions.None;
                options.TimeoutMs = 2_000;
            });

        await using var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        var result = await builder
            .From("sender@example.com")
            .To("recipient@example.com")
            .Subject("Unreachable")
            .TextBody("body")
            .SendAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("SMTP send failed");
    }
}

[CollectionDefinition(nameof(MailpitCollection))]
public sealed class MailpitCollection : ICollectionFixture<MailpitFixture>;
