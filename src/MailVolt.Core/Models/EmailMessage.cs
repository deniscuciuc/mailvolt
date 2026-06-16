namespace MailVolt.Core.Models;

/// <summary>
/// An immutable representation of an email message.
/// </summary>
public sealed record EmailMessage
{
    private static readonly IReadOnlyDictionary<string, string> DefaultHeaders = new Dictionary<string, string>();
    /// <summary>
    /// The sender address. When null, a default will be applied by <see cref="MailVolt.Core.Interfaces.IEmailBuilder"/>.
    /// </summary>
    public EmailAddress? From { get; init; }

    /// <summary>
    /// The primary recipients.
    /// </summary>
    public IReadOnlyList<EmailAddress> To { get; init; } = [];

    /// <summary>
    /// Carbon-copy recipients.
    /// </summary>
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];

    /// <summary>
    /// Blind carbon-copy recipients.
    /// </summary>
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];

    /// <summary>
    /// The reply-to address.
    /// </summary>
    public EmailAddress? ReplyTo { get; init; }

    /// <summary>
    /// The subject line of the email.
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// The plain-text body.
    /// </summary>
    public string? TextBody { get; init; }

    /// <summary>
    /// The HTML body.
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    /// The priority of the email.
    /// </summary>
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;

    /// <summary>
    /// The list of attachments.
    /// </summary>
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];

    /// <summary>
    /// Custom email headers.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = DefaultHeaders;

    /// <summary>
    /// Tags or categories associated with this email.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}
