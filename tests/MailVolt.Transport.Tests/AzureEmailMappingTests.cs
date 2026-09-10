using AwesomeAssertions;
using MailVolt.Core.Models;
using MailVolt.Transport.AzureEmail;
using Xunit;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Covers the Azure Communication Services message mapping, which had no tests — the
/// transport's only coverage was interface and DI-registration shape.
/// </summary>
public sealed class AzureEmailMappingTests
{
    [Fact]
    public void Addresses_subject_and_bodies_are_mapped()
    {
        var email = Email() with
        {
            From = new EmailAddress("from@example.com", "From Name"),
            To = [new EmailAddress("to@example.com", "To Name")],
            Cc = [new EmailAddress("cc@example.com")],
            Bcc = [new EmailAddress("bcc@example.com")],
            TextBody = "plain",
            HtmlBody = "<p>rich</p>",
        };

        var message = AzureEmailSender.BuildAzureMessage(email);

        message.SenderAddress.Should().Be("from@example.com");
        message.Recipients.To.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Address = "to@example.com", DisplayName = "To Name" });
        message.Recipients.CC.Should().ContainSingle().Which.Address.Should().Be("cc@example.com");
        message.Recipients.BCC.Should().ContainSingle().Which.Address.Should().Be("bcc@example.com");
        message.Content.Subject.Should().Be("Subject");
        message.Content.PlainText.Should().Be("plain");
        message.Content.Html.Should().Be("<p>rich</p>");
    }

    [Fact]
    public void A_missing_From_throws_a_clear_error()
    {
        var act = () => AzureEmailSender.BuildAzureMessage(Email() with { From = null });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*From address is required*");
    }

    [Fact]
    public void ReplyTo_is_mapped()
    {
        // Previously dropped entirely on this transport.
        var email = Email() with { ReplyTo = new EmailAddress("reply@example.com", "Reply Name") };

        var message = AzureEmailSender.BuildAzureMessage(email);

        message.ReplyTo.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Address = "reply@example.com", DisplayName = "Reply Name" });
    }

    [Fact]
    public void An_attachment_is_mapped_with_its_content_and_type()
    {
        var email = Email() with
        {
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = "report.pdf",
                    Content = "pdf-bytes"u8.ToArray(),
                    ContentType = "application/pdf",
                },
            ],
        };

        var message = AzureEmailSender.BuildAzureMessage(email);

        var attachment = message.Attachments.Should().ContainSingle().Subject;
        attachment.Name.Should().Be("report.pdf");
        attachment.ContentType.Should().Be("application/pdf");
        attachment.Content.ToArray().Should().Equal("pdf-bytes"u8.ToArray());
        attachment.ContentId.Should().BeNull();
    }

    [Fact]
    public void An_inline_attachment_carries_its_content_id()
    {
        // Previously sent as an ordinary attachment, so a cid: reference never resolved.
        var email = Email() with
        {
            HtmlBody = """<img src="cid:logo@mailvolt" />""",
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = "logo.png",
                    Content = "png"u8.ToArray(),
                    ContentType = "image/png",
                    ContentId = "logo@mailvolt",
                },
            ],
        };

        var message = AzureEmailSender.BuildAzureMessage(email);

        message.Attachments.Should().ContainSingle()
            .Which.ContentId.Should().Be("logo@mailvolt");
    }

    [Fact]
    public void Custom_headers_are_mapped()
    {
        var email = Email() with
        {
            Headers = new Dictionary<string, string> { ["X-Campaign-Id"] = "welcome" },
        };

        var message = AzureEmailSender.BuildAzureMessage(email);

        message.Headers.Should().ContainKey("X-Campaign-Id").WhoseValue.Should().Be("welcome");
    }

    [Fact]
    public void An_empty_body_is_left_unset_rather_than_sent_as_an_empty_string()
    {
        var email = Email() with { TextBody = string.Empty, HtmlBody = string.Empty };

        var message = AzureEmailSender.BuildAzureMessage(email);

        message.Content.PlainText.Should().BeNull();
        message.Content.Html.Should().BeNull();
    }

    private static EmailMessage Email() => new()
    {
        From = new EmailAddress("from@example.com"),
        To = [new EmailAddress("to@example.com")],
        Subject = "Subject",
        TextBody = "body",
    };
}
