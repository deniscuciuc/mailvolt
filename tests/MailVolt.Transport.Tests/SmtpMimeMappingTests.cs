using AwesomeAssertions;
using MailVolt.Core.Models;
using MailVolt.Transport.Smtp;
using MimeKit;
using MimeKit.Text;
using Xunit;

namespace MailVolt.Transport.Tests;

/// <summary>
/// Covers <c>SmtpSender.BuildMimeMessage</c>, which had no tests at all — the SMTP transport's
/// only coverage was "implements ISender" and DI-registration shape.
/// </summary>
public sealed class SmtpMimeMappingTests
{
    [Fact]
    public void Addresses_and_display_names_are_mapped()
    {
        var email = Email() with
        {
            From = new EmailAddress("from@example.com", "From Name"),
            To = [new EmailAddress("to@example.com", "To Name")],
            Cc = [new EmailAddress("cc@example.com")],
            Bcc = [new EmailAddress("bcc@example.com")],
            ReplyTo = new EmailAddress("reply@example.com", "Reply Name"),
        };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.From.Mailboxes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Address = "from@example.com", Name = "From Name" });
        message.To.Mailboxes.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Address = "to@example.com", Name = "To Name" });
        message.Cc.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("cc@example.com");
        message.Bcc.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("bcc@example.com");
        message.ReplyTo.Mailboxes.Should().ContainSingle().Which.Address.Should().Be("reply@example.com");
    }

    [Fact]
    public void Multiple_recipients_are_all_mapped()
    {
        var email = Email() with
        {
            To = [new EmailAddress("a@example.com"), new EmailAddress("b@example.com")],
            Cc = [new EmailAddress("c@example.com"), new EmailAddress("d@example.com")],
        };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.To.Mailboxes.Should().HaveCount(2);
        message.Cc.Mailboxes.Should().HaveCount(2);
    }

    [Fact]
    public void Subject_is_mapped()
    {
        using var message = SmtpSender.BuildMimeMessage(Email() with { Subject = "Hello there" });

        message.Subject.Should().Be("Hello there");
    }

    [Fact]
    public void Text_and_html_bodies_become_a_multipart_alternative()
    {
        var email = Email() with { TextBody = "plain text", HtmlBody = "<p>rich text</p>" };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.Body.Should().BeOfType<MultipartAlternative>();
        message.TextBody.Should().Be("plain text");
        message.HtmlBody.Should().Be("<p>rich text</p>");
    }

    [Fact]
    public void A_text_only_body_is_a_plain_text_part()
    {
        var email = Email() with { TextBody = "plain only", HtmlBody = null };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.Body.Should().BeOfType<TextPart>();
        message.TextBody.Should().Be("plain only");
        message.HtmlBody.Should().BeNull();
    }

    [Fact]
    public void An_html_only_body_is_marked_as_html()
    {
        var email = Email() with { TextBody = null, HtmlBody = "<p>html only</p>" };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.HtmlBody.Should().Be("<p>html only</p>");
        message.Body.Should().BeOfType<TextPart>()
            .Which.ContentType.IsMimeType("text", "html").Should().BeTrue();
    }

    [Fact]
    public void A_regular_attachment_becomes_a_mime_attachment()
    {
        var email = Email() with
        {
            TextBody = "see attached",
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

        using var message = SmtpSender.BuildMimeMessage(email);

        var attachment = message.Attachments.Should().ContainSingle().Subject;
        attachment.ContentDisposition!.FileName.Should().Be("report.pdf");
        attachment.ContentType.IsMimeType("application", "pdf").Should().BeTrue();
    }

    [Fact]
    public void An_inline_attachment_becomes_a_linked_resource_with_its_content_id()
    {
        var email = Email() with
        {
            HtmlBody = """<img src="cid:logo@mailvolt" />""",
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = "logo.png",
                    Content = "png-bytes"u8.ToArray(),
                    ContentType = "image/png",
                    ContentId = "logo@mailvolt",
                },
            ],
        };

        using var message = SmtpSender.BuildMimeMessage(email);

        // A linked resource renders in the mail client; a plain attachment shows up as a
        // downloadable file instead. MimeKit nests the related part inside an alternative
        // when both bodies are present, so assert on the structure rather than the root.
        // Structure is MultipartAlternative[TextPart, MultipartRelated[TextPart, image]].
        var related = message.Body.Should().BeOfType<MultipartAlternative>().Subject
            .OfType<MultipartRelated>().Should().ContainSingle().Subject;

        related.OfType<MimePart>()
            .Should().ContainSingle(part => part.ContentId == "logo@mailvolt")
            .Which.ContentType.IsMimeType("image", "png").Should().BeTrue();
        message.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void Inline_and_regular_attachments_can_coexist()
    {
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
                new EmailAttachment
                {
                    FileName = "report.csv",
                    Content = "csv"u8.ToArray(),
                    ContentType = "text/csv",
                },
            ],
        };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.Attachments.Should().ContainSingle()
            .Which.ContentDisposition!.FileName.Should().Be("report.csv");
        message.BodyParts.OfType<MimePart>()
            .Should().Contain(part => part.ContentId == "logo@mailvolt");
    }

    [Theory]
    [InlineData(EmailPriority.Low, "5")]
    [InlineData(EmailPriority.Normal, "3")]
    [InlineData(EmailPriority.High, "1")]
    public void Priority_maps_to_the_X_Priority_header(EmailPriority priority, string expected)
    {
        using var message = SmtpSender.BuildMimeMessage(Email() with { Priority = priority });

        message.Headers["X-Priority"].Should().Be(expected);
    }

    [Fact]
    public void Custom_headers_are_mapped()
    {
        var email = Email() with
        {
            Headers = new Dictionary<string, string>
            {
                ["X-Campaign-Id"] = "welcome-2026",
                ["X-Tenant"] = "acme",
            },
        };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.Headers["X-Campaign-Id"].Should().Be("welcome-2026");
        message.Headers["X-Tenant"].Should().Be("acme");
    }

    [Fact]
    public void A_custom_X_Priority_header_wins_over_the_mapped_priority()
    {
        // Custom headers are applied after the priority mapping, so an explicit header is
        // the escape hatch for a provider that expects a different scale.
        var email = Email() with
        {
            Priority = EmailPriority.High,
            Headers = new Dictionary<string, string> { ["X-Priority"] = "2" },
        };

        using var message = SmtpSender.BuildMimeMessage(email);

        message.Headers["X-Priority"].Should().Be("2");
    }

    [Fact]
    public void A_message_with_no_From_maps_to_an_empty_From_list()
    {
        // The builder normally guarantees a From, but the mapping must not throw for a
        // message constructed directly.
        using var message = SmtpSender.BuildMimeMessage(Email() with { From = null });

        message.From.Mailboxes.Should().BeEmpty();
    }

    private static EmailMessage Email() => new()
    {
        From = new EmailAddress("from@example.com"),
        To = [new EmailAddress("to@example.com")],
        Subject = "Subject",
        TextBody = "body",
    };
}
