using FluentAssertions;
using MailVolt.Core.Models;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class EmailMessageTests
{
    [Fact]
    public void Default_values_are_empty_or_null()
    {
        var message = new EmailMessage();

        message.From.Should().BeNull();
        message.To.Should().BeEmpty();
        message.Cc.Should().BeEmpty();
        message.Bcc.Should().BeEmpty();
        message.ReplyTo.Should().BeNull();
        message.Subject.Should().Be(string.Empty);
        message.TextBody.Should().BeNull();
        message.HtmlBody.Should().BeNull();
        message.Priority.Should().Be(EmailPriority.Normal);
        message.Attachments.Should().BeEmpty();
        message.Headers.Should().BeEmpty();
        message.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Can_set_all_properties_via_init()
    {
        var from = new EmailAddress("from@example.com", "Sender");
        var to = new List<EmailAddress> { new("to@example.com") }.AsReadOnly();
        var cc = new List<EmailAddress> { new("cc@example.com") }.AsReadOnly();
        var bcc = new List<EmailAddress> { new("bcc@example.com") }.AsReadOnly();
        var replyTo = new EmailAddress("reply@example.com");
        var attachments = new List<EmailAttachment>
        {
            new()
            {
                FileName = "doc.pdf",
                Content = new MemoryStream(),
                ContentType = "application/pdf",
            },
        }.AsReadOnly();
        var headers = new Dictionary<string, string> { ["X-Custom"] = "value" };
        var tags = new List<string> { "important" }.AsReadOnly();

        var message = new EmailMessage
        {
            From = from,
            To = to,
            Cc = cc,
            Bcc = bcc,
            ReplyTo = replyTo,
            Subject = "Hello",
            TextBody = "plain text",
            HtmlBody = "<p>HTML</p>",
            Priority = EmailPriority.High,
            Attachments = attachments,
            Headers = headers,
            Tags = tags,
        };

        message.From.Should().Be(from);
        message.To.Should().BeEquivalentTo(to);
        message.Cc.Should().BeEquivalentTo(cc);
        message.Bcc.Should().BeEquivalentTo(bcc);
        message.ReplyTo.Should().Be(replyTo);
        message.Subject.Should().Be("Hello");
        message.TextBody.Should().Be("plain text");
        message.HtmlBody.Should().Be("<p>HTML</p>");
        message.Priority.Should().Be(EmailPriority.High);
        message.Attachments.Should().HaveCount(1);
        message.Headers.Should().ContainKey("X-Custom");
        message.Tags.Should().Contain("important");
    }

    [Fact]
    public void Record_equality_compares_all_properties()
    {
        var msg1 = new EmailMessage { Subject = "Test", From = new EmailAddress("a@b.com") };
        var msg2 = new EmailMessage { Subject = "Test", From = new EmailAddress("a@b.com") };
        var msg3 = new EmailMessage { Subject = "Different", From = new EmailAddress("a@b.com") };

        msg1.Should().Be(msg2);
        msg1.Should().NotBe(msg3);
    }

    [Fact]
    public void Collections_are_immutable()
    {
        var message = new EmailMessage
        {
            To = new List<EmailAddress> { new("a@b.com") }.AsReadOnly(),
            Attachments = new List<EmailAttachment>().AsReadOnly(),
            Headers = new Dictionary<string, string>(),
            Tags = new List<string>().AsReadOnly(),
        };

        message.To.Should().BeAssignableTo<IReadOnlyList<EmailAddress>>();
        message.Attachments.Should().BeAssignableTo<IReadOnlyList<EmailAttachment>>();
        message.Headers.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
        message.Tags.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Theory]
    [InlineData(EmailPriority.Low)]
    [InlineData(EmailPriority.Normal)]
    [InlineData(EmailPriority.High)]
    public void Priority_enum_values_are_valid(EmailPriority priority)
    {
        var message = new EmailMessage { Priority = priority };
        message.Priority.Should().Be(priority);
    }
}
