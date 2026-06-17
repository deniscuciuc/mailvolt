using System.Net;
using System.Net.Http;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.SendGrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGridEmailAddress = SendGrid.Helpers.Mail.EmailAddress;
using CoreEmailAddress = MailVolt.Core.Models.EmailAddress;

namespace MailVolt.Transport.Tests;

public sealed class SendGridSenderTests
{
    private static readonly SendGridSenderOptions Options = new()
    {
        ApiKey = "my-sendgrid-api-key",
        BaseUrl = "https://api.sendgrid.com"
    };

    private static ILogger<SendGridSender> CreateLogger()
        => NullLogger<SendGridSender>.Instance;

    private static SendGridSenderOptions CloneOptions(Action<SendGridSenderOptions>? configure = null)
    {
        var clone = new SendGridSenderOptions
        {
            ApiKey = Options.ApiKey,
            BaseUrl = Options.BaseUrl
        };
        configure?.Invoke(clone);
        return clone;
    }

    private static Response CreateSuccessResponse(string? messageId = null)
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.Accepted);
        if (!string.IsNullOrEmpty(messageId))
        {
            responseMessage.Headers.Add("X-Message-Id", messageId);
        }

        return new Response(HttpStatusCode.Accepted, new StringContent(""), responseMessage.Headers);
    }

    private static Response CreateFailureResponse(HttpStatusCode statusCode, string body)
    {
        var responseMessage = new HttpResponseMessage(statusCode);
        return new Response(statusCode, new StringContent(body), responseMessage.Headers);
    }

    [Fact]
    public void SendGridSender_implements_ISendGridSender()
    {
        typeof(SendGridSender).Should().Implement<ISendGridSender>();
    }

    [Fact]
    public void ISendGridSender_extends_ISender()
    {
        typeof(ISendGridSender).Should().Implement<ISender>();
    }

    [Fact]
    public void Constructor_throws_when_client_is_null()
    {
        var act = () => new SendGridSender(null!, Helpers.OptionsOf(Options), CreateLogger());
        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void Constructor_throws_when_options_is_null()
    {
        var act = () => new SendGridSender(Substitute.For<ISendGridClient>(), null!, CreateLogger());
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_throws_when_logger_is_null()
    {
        var act = () => new SendGridSender(Substitute.For<ISendGridClient>(), Helpers.OptionsOf(Options), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void MapToSendGridMessage_sets_from_and_subject()
    {
        var email = Helpers.CreateTestEmail();

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.From.Should().BeEquivalentTo(new SendGridEmailAddress("sender@example.com", "Sender"));
        message.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public void MapToSendGridMessage_sets_to_cc_bcc_and_replyTo()
    {
        var email = Helpers.CreateTestEmail() with
        {
            To = [new CoreEmailAddress("to@example.com", "To")],
            Cc = [new CoreEmailAddress("cc@example.com", "Cc")],
            Bcc = [new CoreEmailAddress("bcc@example.com", "Bcc")],
            ReplyTo = new CoreEmailAddress("reply@example.com", "Reply")
        };

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.Personalizations.Should().ContainSingle();
        var personalization = message.Personalizations[0];
        personalization.Tos.Should().ContainSingle().Which.Should().BeEquivalentTo(new SendGridEmailAddress("to@example.com", "To"));
        personalization.Ccs.Should().ContainSingle().Which.Should().BeEquivalentTo(new SendGridEmailAddress("cc@example.com", "Cc"));
        personalization.Bccs.Should().ContainSingle().Which.Should().BeEquivalentTo(new SendGridEmailAddress("bcc@example.com", "Bcc"));
        message.ReplyTo.Should().BeEquivalentTo(new SendGridEmailAddress("reply@example.com", "Reply"));
    }

    [Fact]
    public void MapToSendGridMessage_sets_text_and_html_content()
    {
        var email = Helpers.CreateTestEmail();

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.Contents.Should().Contain(c => c.Type == "text/plain" && c.Value == "Hello plain text");
        message.Contents.Should().Contain(c => c.Type == "text/html" && c.Value == "<p>Hello HTML</p>");
    }

    [Fact]
    public void MapToSendGridMessage_omits_content_when_dynamic_template_is_enabled_and_no_body()
    {
        var email = Helpers.CreateTestEmail() with { TextBody = null, HtmlBody = null };
        var options = CloneOptions(o => o.UseDynamicTemplates = true);

        var message = SendGridSender.MapToSendGridMessage(email, options);

        message.Subject.Should().BeNull();
        message.Contents.Should().BeNullOrEmpty();
    }

    [Fact]
    public void MapToSendGridMessage_sets_attachment_with_attachment_disposition()
    {
        var email = Helpers.CreateTestEmailWithAttachment();

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.Attachments.Should().ContainSingle();
        var attachment = message.Attachments![0];
        attachment.Filename.Should().Be("report.pdf");
        attachment.Type.Should().Be("application/pdf");
        attachment.Disposition.Should().Be("attachment");
        attachment.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MapToSendGridMessage_sets_inline_attachment_with_inline_disposition()
    {
        var email = Helpers.CreateTestEmailWithInlineAttachment();

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.Attachments.Should().ContainSingle();
        var attachment = message.Attachments![0];
        attachment.Filename.Should().Be("logo.png");
        attachment.Type.Should().Be("image/png");
        attachment.Disposition.Should().Be("inline");
        attachment.ContentId.Should().Be("logo@mailvolt");
        attachment.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MapToSendGridMessage_sets_categories_from_tags()
    {
        var email = Helpers.CreateTestEmail() with { Tags = ["transactional", "receipt"] };

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.Categories.Should().BeEquivalentTo(["transactional", "receipt"]);
    }

    [Fact]
    public void MapToSendGridMessage_sets_sandbox_mode_when_enabled()
    {
        var email = Helpers.CreateTestEmail();
        var options = CloneOptions(o => o.SandboxMode = "true");

        var message = SendGridSender.MapToSendGridMessage(email, options);

        message.MailSettings.Should().NotBeNull();
        message.MailSettings!.SandboxMode.Should().NotBeNull();
        message.MailSettings.SandboxMode!.Enable.Should().BeTrue();
    }

    [Fact]
    public void MapToSendGridMessage_sets_global_headers()
    {
        var email = Helpers.CreateTestEmail() with
        {
            Headers = new Dictionary<string, string> { ["X-Custom-Header"] = "custom-value" }
        };

        var message = SendGridSender.MapToSendGridMessage(email, Options);

        message.Headers.Should().ContainKey("X-Custom-Header").WhoseValue.Should().Be("custom-value");
    }

    [Fact]
    public async Task SendAsync_sends_message_via_client()
    {
        var client = Substitute.For<ISendGridClient>();
        client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResponse());

        var sender = new SendGridSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        await sender.SendAsync(email);

        await client.Received(1).SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_returns_success_with_messageId()
    {
        var client = Substitute.For<ISendGridClient>();
        client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
            .Returns(CreateSuccessResponse("sg-message-987"));

        var sender = new SendGridSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("sg-message-987");
    }

    [Fact]
    public async Task SendAsync_returns_failure_on_error_status()
    {
        var client = Substitute.For<ISendGridClient>();
        client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
            .Returns(CreateFailureResponse(HttpStatusCode.Forbidden, "{\"errors\":[{\"message\":\"Forbidden\"}]}"));

        var sender = new SendGridSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("403");
    }

    [Fact]
    public async Task SendAsync_throws_OperationCanceledException_when_cancelled()
    {
        var client = Substitute.For<ISendGridClient>();
        client.SendEmailAsync(Arg.Any<SendGridMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Response>(new OperationCanceledException()));

        var sender = new SendGridSender(client, Helpers.OptionsOf(Options), CreateLogger());
        var email = Helpers.CreateTestEmail();

        Func<Task> act = () => sender.SendAsync(email);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
