using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using MailVolt.Core.Models;

namespace MailVolt.Testing;

/// <summary>
/// FluentAssertions extension methods for <see cref="InMemorySender"/>.
/// </summary>
public static class InMemorySenderExtensions
{
    /// <summary>Returns assertion helpers for an <see cref="InMemorySender"/>.</summary>
    public static SentEmailAssertions Should(this InMemorySender sender) =>
        new(sender);
}

/// <summary>
/// Provides fluent assertion methods for verifying emails captured by <see cref="InMemorySender"/>.
/// </summary>
public sealed class SentEmailAssertions(InMemorySender sender) : ReferenceTypeAssertions<InMemorySender, SentEmailAssertions>(sender, AssertionChain.GetOrCreate())
{
    /// <inheritdoc />
    protected override string Identifier => "InMemorySender";

    /// <summary>Asserts that exactly <paramref name="count"/> emails were sent.</summary>
    public SentEmailAssertions HaveCount(int count)
    {
        Subject.SentCount.Should().Be(count, "expected {0} emails to be sent", count);
        return this;
    }

    /// <summary>Asserts that at least one email was sent to the specified address.</summary>
    public SentEmailAssertions ContainEmailTo(string address)
    {
        Subject.SentEmails.Should()
            .Contain(s => s.Email.To.Any(a => a.Address.Equals(address, StringComparison.OrdinalIgnoreCase)),
            "expected an email to {0}", address);
        return this;
    }

    /// <summary>Asserts that at least one email has a subject containing the specified text.</summary>
    public SentEmailAssertions ContainSubject(string subject)
    {
        Subject.SentEmails.Should()
            .Contain(s => s.Email.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase),
            "expected subject containing '{0}'", subject);
        return this;
    }

    /// <summary>Asserts that at least one email has an HTML body containing the specified content.</summary>
    public SentEmailAssertions ContainHtmlBody(string content)
    {
        Subject.SentEmails.Should()
            .Contain(s => s.Email.HtmlBody != null &&
                         s.Email.HtmlBody.Contains(content, StringComparison.OrdinalIgnoreCase),
            "expected HTML body containing '{0}'", content);
        return this;
    }

    /// <summary>Asserts that no emails have been sent.</summary>
    public SentEmailAssertions HaveNoEmailsSent()
    {
        Subject.SentCount.Should().Be(0, "expected no emails to be sent");
        return this;
    }

    /// <summary>Asserts that at least one email has an attachment with the specified file name.</summary>
    public SentEmailAssertions ContainAttachment(string fileName)
    {
        Subject.SentEmails.Should()
            .Contain(s => s.Email.Attachments.Any(a => a.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)),
            "expected attachment '{0}'", fileName);
        return this;
    }
}
