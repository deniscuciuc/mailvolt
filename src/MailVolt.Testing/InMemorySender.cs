using System.Collections.Concurrent;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;

namespace MailVolt.Testing;

/// <summary>
/// ISender implementation that captures emails in memory instead of sending.
/// Thread-safe. Register as Singleton in test DI.
/// </summary>
public sealed class InMemorySender : ISender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    /// <summary>All emails that have been sent through this sender.</summary>
    public IReadOnlyList<SentEmail> SentEmails => [.. _sent];

    /// <summary>Total number of emails sent.</summary>
    public int SentCount => _sent.Count;

    /// <inheritdoc />
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(email, DateTimeOffset.UtcNow));
        return Task.FromResult(EmailResult.Success(Guid.NewGuid().ToString()));
    }

    /// <summary>Clear all captured emails.</summary>
    public void Clear() => _sent.Clear();
}

/// <summary>Represents an email captured by <see cref="InMemorySender"/>.</summary>
/// <param name="Email">The email message that was sent.</param>
/// <param name="SentAt">The UTC timestamp when the email was sent.</param>
public sealed record SentEmail(EmailMessage Email, DateTimeOffset SentAt);
