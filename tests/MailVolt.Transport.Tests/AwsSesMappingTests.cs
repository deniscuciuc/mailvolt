using AwesomeAssertions;
using MailVolt.Core.Models;
using MailVolt.Transport.AwsSes;
using MimeKit;
using Xunit;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Covers the AWS SES request mapping, which had no tests — the transport's only coverage
/// was interface and DI-registration shape.
/// </summary>
public sealed class AwsSesMappingTests
{
    [Fact]
    public void A_message_without_attachments_maps_to_a_structured_request()
    {
        var email = Email() with
        {
            Cc = [new EmailAddress("cc@example.com")],
            Bcc = [new EmailAddress("bcc@example.com")],
            TextBody = "plain",
            HtmlBody = "<p>rich</p>",
        };

        var request = AwsSesSender.BuildSimpleRequest(email, configurationSetName: null);

        request.FromEmailAddress.Should().Be("from@example.com");
        request.Destination.ToAddresses.Should().ContainSingle().Which.Should().Be("to@example.com");
        request.Destination.CcAddresses.Should().ContainSingle().Which.Should().Be("cc@example.com");
        request.Destination.BccAddresses.Should().ContainSingle().Which.Should().Be("bcc@example.com");
        request.Content.Simple.Subject.Data.Should().Be("Subject");
        request.Content.Simple.Body.Text.Data.Should().Be("plain");
        request.Content.Simple.Body.Html.Data.Should().Be("<p>rich</p>");
    }

    [Fact]
    public void A_display_name_is_included_in_the_mapped_address()
    {
        var email = Email() with { From = new EmailAddress("from@example.com", "From Name") };

        var request = AwsSesSender.BuildSimpleRequest(email, null);

        request.FromEmailAddress.Should().Be("From Name <from@example.com>");
    }

    [Fact]
    public void The_configuration_set_name_is_passed_through()
    {
        var request = AwsSesSender.BuildSimpleRequest(Email(), "my-config-set");

        request.ConfigurationSetName.Should().Be("my-config-set");
    }

    [Fact]
    public void A_missing_body_maps_to_null_rather_than_an_empty_content()
    {
        var email = Email() with { TextBody = "text-only", HtmlBody = null };

        var request = AwsSesSender.BuildSimpleRequest(email, null);

        request.Content.Simple.Body.Text.Data.Should().Be("text-only");
        request.Content.Simple.Body.Html.Should().BeNull();
    }

    [Fact]
    public void ReplyTo_is_mapped_on_the_structured_request()
    {
        // Previously dropped entirely on this path.
        var email = Email() with { ReplyTo = new EmailAddress("reply@example.com") };

        var request = AwsSesSender.BuildSimpleRequest(email, null);

        request.ReplyToAddresses.Should().ContainSingle().Which.Should().Be("reply@example.com");
    }

    [Fact]
    public void The_raw_mime_message_maps_addresses_subject_and_bodies()
    {
        var email = Email() with
        {
            From = new EmailAddress("from@example.com", "From Name"),
            Cc = [new EmailAddress("cc@example.com")],
            TextBody = "plain",
            HtmlBody = "<p>rich</p>",
            Attachments = [Attachment("report.pdf", "application/pdf")],
        };

        using var message = AwsSesSender.BuildMimeMessage(email);

        message.From.Mailboxes.Should().ContainSingle()
            .Which.Name.Should().Be("From Name");
        message.To.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("to@example.com");
        message.Cc.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("cc@example.com");
        message.Subject.Should().Be("Subject");
        message.TextBody.Should().Be("plain");
        message.HtmlBody.Should().Be("<p>rich</p>");
        message.Attachments.Should().ContainSingle()
            .Which.ContentDisposition!.FileName.Should().Be("report.pdf");
    }

    [Fact]
    public void The_raw_mime_message_maps_ReplyTo()
    {
        // Previously dropped, so a Reply-To was silently lost whenever the email had an
        // attachment and SES took the raw path.
        var email = Email() with
        {
            ReplyTo = new EmailAddress("reply@example.com", "Reply Name"),
            Attachments = [Attachment("a.txt", "text/plain")],
        };

        using var message = AwsSesSender.BuildMimeMessage(email);

        message.ReplyTo.Mailboxes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Address = "reply@example.com", Name = "Reply Name" });
    }

    [Fact]
    public void An_inline_attachment_becomes_a_linked_resource()
    {
        // Previously added as an ordinary attachment, so a cid: reference in the HTML never
        // resolved and the image appeared as a download.
        var email = Email() with
        {
            TextBody = null,
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

        using var message = AwsSesSender.BuildMimeMessage(email);

        message.Body.Should().BeOfType<MultipartRelated>()
            .Subject.OfType<MimePart>()
            .Should().ContainSingle(part => part.ContentId == "logo@mailvolt");
        message.Attachments.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EmailPriority.Low, "5")]
    [InlineData(EmailPriority.Normal, "3")]
    [InlineData(EmailPriority.High, "1")]
    public void The_raw_mime_message_maps_priority(EmailPriority priority, string expected)
    {
        // Previously never set on this path.
        var email = Email() with
        {
            Priority = priority,
            Attachments = [Attachment("a.txt", "text/plain")],
        };

        using var message = AwsSesSender.BuildMimeMessage(email);

        message.Headers["X-Priority"].Should().Be(expected);
    }

    [Fact]
    public void The_raw_mime_message_maps_custom_headers()
    {
        var email = Email() with
        {
            Headers = new Dictionary<string, string> { ["X-Campaign-Id"] = "welcome" },
            Attachments = [Attachment("a.txt", "text/plain")],
        };

        using var message = AwsSesSender.BuildMimeMessage(email);

        message.Headers["X-Campaign-Id"].Should().Be("welcome");
    }

    private static EmailAttachment Attachment(string fileName, string contentType) => new()
    {
        FileName = fileName,
        Content = "content"u8.ToArray(),
        ContentType = contentType,
    };

    private static EmailMessage Email() => new()
    {
        From = new EmailAddress("from@example.com"),
        To = [new EmailAddress("to@example.com")],
        Subject = "Subject",
        TextBody = "body",
    };
}
