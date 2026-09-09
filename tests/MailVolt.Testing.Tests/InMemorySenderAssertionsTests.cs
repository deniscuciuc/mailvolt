using AwesomeAssertions;
using MailVolt.Core.Models;
using MailVolt.Core.Transports;
using Xunit;

namespace MailVolt.Testing.Tests;

/// <summary>
/// The assertion helpers are shipped public API but had no tests at all. Each one is checked
/// both ways: a passing case, and a failing case that proves the assertion actually fails
/// when it should — an assertion that never fails is worse than no assertion.
/// </summary>
public sealed class InMemorySenderAssertionsTests
{
    [Fact]
    public async Task HaveCount_passes_for_the_right_count()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email());
        await sender.SendAsync(Email());

        sender.Should().HaveCount(2);
    }

    [Fact]
    public async Task HaveCount_fails_for_the_wrong_count()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email());

        var act = () => sender.Should().HaveCount(2);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task ContainEmailTo_matches_case_insensitively()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with { To = [new EmailAddress("User@Example.com")] });

        sender.Should().ContainEmailTo("user@example.com");
    }

    [Fact]
    public async Task ContainEmailTo_fails_for_an_address_that_was_not_used()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email());

        var act = () => sender.Should().ContainEmailTo("nobody@example.com");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task ContainSubject_matches_a_substring()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with { Subject = "Welcome to MailVolt" });

        sender.Should().ContainSubject("welcome");
    }

    [Fact]
    public async Task ContainSubject_fails_when_no_subject_matches()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with { Subject = "Welcome" });

        var act = () => sender.Should().ContainSubject("Goodbye");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task ContainHtmlBody_matches_a_substring()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with { HtmlBody = "<h1>Hello there</h1>" });

        sender.Should().ContainHtmlBody("hello");
    }

    [Fact]
    public async Task ContainHtmlBody_fails_when_the_email_has_no_html_body()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with { HtmlBody = null });

        var act = () => sender.Should().ContainHtmlBody("anything");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void HaveNoEmailsSent_passes_for_a_fresh_sender()
    {
        new InMemorySender().Should().HaveNoEmailsSent();
    }

    [Fact]
    public async Task HaveNoEmailsSent_fails_once_something_was_sent()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email());

        var act = () => sender.Should().HaveNoEmailsSent();

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task ContainAttachment_matches_by_file_name()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with
        {
            Attachments =
            [
                new EmailAttachment
                {
                    FileName = "Report.PDF",
                    Content = "x"u8.ToArray(),
                    ContentType = "application/pdf",
                },
            ],
        });

        sender.Should().ContainAttachment("report.pdf");
    }

    [Fact]
    public async Task ContainAttachment_fails_when_the_attachment_is_missing()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email());

        var act = () => sender.Should().ContainAttachment("report.pdf");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public async Task Assertions_chain()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email() with
        {
            To = [new EmailAddress("user@example.com")],
            Subject = "Welcome",
            HtmlBody = "<p>Hi</p>",
        });

        sender.Should()
            .HaveCount(1)
            .ContainEmailTo("user@example.com")
            .ContainSubject("Welcome")
            .ContainHtmlBody("Hi");
    }

    private static EmailMessage Email() => new()
    {
        From = new EmailAddress("from@example.com"),
        To = [new EmailAddress("to@example.com")],
        Subject = "Subject",
        TextBody = "body",
    };
}
