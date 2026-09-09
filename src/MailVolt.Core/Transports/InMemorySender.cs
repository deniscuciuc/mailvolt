using System.Collections.Concurrent;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Transports;

namespace MailVolt.Core.Transports;

/// <summary>
/// An <see cref="ISender"/> that captures messages in memory instead of delivering them.
/// </summary>
/// <remarks>
/// Useful for tests, but also for a local or staging environment that should not send real
/// mail — which is why it lives in <c>MailVolt.Core</c> rather than the testing package, so
/// selecting the <c>InMemory</c> transport does not pull a test dependency into a
/// production application. Thread-safe; register as a singleton.
/// </remarks>
public sealed class InMemorySender : ISender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    /// <summary>All emails that have been sent through this sender, oldest first.</summary>
    public IReadOnlyList<SentEmail> SentEmails => [.. _sent];

    /// <summary>Total number of emails sent.</summary>
    public int SentCount => _sent.Count;

    /// <inheritdoc />
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        cancellationToken.ThrowIfCancellationRequested();

        _sent.Enqueue(new SentEmail(email, DateTimeOffset.UtcNow));
        return Task.FromResult(EmailResult.Success(Guid.NewGuid().ToString()));
    }

    /// <summary>Clears all captured emails.</summary>
    public void Clear() => _sent.Clear();
}

/// <summary>Represents an email captured by <see cref="InMemorySender"/>.</summary>
/// <param name="Email">The email message that was sent.</param>
/// <param name="SentAt">The UTC timestamp when the email was captured.</param>
public sealed record SentEmail(EmailMessage Email, DateTimeOffset SentAt);
