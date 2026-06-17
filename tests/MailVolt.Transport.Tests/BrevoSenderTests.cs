using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Brevo;
using MailVolt.Transport.Brevo.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using brevo_csharp.Api;
using brevo_csharp.Model;
using Task = System.Threading.Tasks.Task;

namespace MailVolt.Transport.Tests;

public sealed class BrevoSenderTests
{
    private static readonly BrevoSenderOptions Options = new()
    {
        ApiKey = "my-brevo-api-key"
    };

    [Fact]
    public void BrevoSender_implements_IBrevoSender()
    {
        typeof(BrevoSender).Should().Implement<IBrevoSender>();
    }

    [Fact]
    public void IBrevoSender_extends_ISender()
    {
        typeof(IBrevoSender).Should().Implement<ISender>();
    }

    [Fact]
    public void Constructor_throws_on_null_options()
    {
        var act = () => new BrevoSender((IOptions<BrevoSenderOptions>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddBrevoSender_with_delegate_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        builder.AddBrevoSender(opts =>
        {
            opts.ApiKey = "brevo-key";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<BrevoSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<BrevoSenderOptions>>().Value;
        resolvedOptions.ApiKey.Should().Be("brevo-key");
    }

    [Fact]
    public void AddBrevoSender_with_configuration_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Brevo:ApiKey"] = "brevo-key"
            })
            .Build();

        builder.AddBrevoSender(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<BrevoSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<BrevoSenderOptions>>().Value;
        resolvedOptions.ApiKey.Should().Be("brevo-key");
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_sender()
    {
        var email = Helpers.CreateTestEmail();

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.Sender.Should().NotBeNull();
        request.Sender!.Email.Should().Be("sender@example.com");
        request.Sender.Name.Should().Be("Sender");
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_recipients()
    {
        var email = Helpers.CreateTestEmail();

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.To.Should().HaveCount(1);
        request.To[0].Email.Should().Be("recipient@example.com");
        request.To[0].Name.Should().Be("Recipient");
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_subject_and_content()
    {
        var email = Helpers.CreateTestEmail();

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.Subject.Should().Be("Test Subject");
        request.HtmlContent.Should().Be("<p>Hello HTML</p>");
        request.TextContent.Should().Be("Hello plain text");
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_reply_to()
    {
        var email = Helpers.CreateTestEmail() with
        {
            ReplyTo = new EmailAddress("reply@example.com", "Reply Person")
        };

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.ReplyTo.Should().NotBeNull();
        request.ReplyTo!.Email.Should().Be("reply@example.com");
        request.ReplyTo.Name.Should().Be("Reply Person");
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_cc_and_bcc()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Cc = [new EmailAddress("cc@example.com", "CC Person")],
            Bcc = [new EmailAddress("bcc@example.com")]
        };

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.Cc.Should().HaveCount(1);
        request.Cc![0].Email.Should().Be("cc@example.com");
        request.Cc[0].Name.Should().Be("CC Person");

        request.Bcc.Should().HaveCount(1);
        request.Bcc![0].Email.Should().Be("bcc@example.com");
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_attachment()
    {
        var email = Helpers.CreateTestEmailWithAttachment();

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.Attachment.Should().HaveCount(1);
        request.Attachment![0].Name.Should().Be("report.pdf");
        request.Attachment[0].Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_headers()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Custom-Header"] = "custom-value"
            }
        };

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.Headers.Should().BeEquivalentTo(email.Headers);
    }

    [Fact]
    public void BuildSendSmtpEmail_maps_tags()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Tags = ["tag1", "tag2"]
        };

        var request = BrevoSender.BuildSendSmtpEmail(email);

        request.Tags.Should().BeEquivalentTo(email.Tags);
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var api = Substitute.For<ITransactionalEmailsApi>();
        api.SendTransacEmailAsync(Arg.Any<SendSmtpEmail>())
            .Returns(new CreateSmtpEmail("brevo-msg-123", null));

        var sender = new BrevoSender(api);
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("brevo-msg-123");
        await api.Received(1).SendTransacEmailAsync(Arg.Is<SendSmtpEmail>(r => r.Subject == "Test Subject"));
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_api_error()
    {
        var api = Substitute.For<ITransactionalEmailsApi>();
        api.SendTransacEmailAsync(Arg.Any<SendSmtpEmail>())
            .Returns(Task.FromException<CreateSmtpEmail>(new brevo_csharp.Client.ApiException(401, "Invalid API key")));

        var sender = new BrevoSender(api);
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid API key");
    }

    [Fact]
    public async Task SendAsync_throws_when_cancellation_is_requested()
    {
        var api = Substitute.For<ITransactionalEmailsApi>();
        var sender = new BrevoSender(api);
        var email = Helpers.CreateTestEmail();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await sender.SendAsync(email, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await api.DidNotReceive().SendTransacEmailAsync(Arg.Any<SendSmtpEmail>());
    }
}
