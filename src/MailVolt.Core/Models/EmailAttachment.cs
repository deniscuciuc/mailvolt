namespace MailVolt.Core.Models;

/// <summary>
/// Represents a file attached to an email message.
/// </summary>
public sealed class EmailAttachment
{
    /// <summary>
    /// The file name of the attachment (e.g. "report.pdf").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The content stream of the attachment.
    /// </summary>
    public required Stream Content { get; init; }

    /// <summary>
    /// The MIME content type (e.g. "application/pdf").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Optional content identifier used for inline images (e.g. "logo@mailvolt").
    /// </summary>
    public string? ContentId { get; init; }

    /// <summary>
    /// Indicates whether this attachment is an inline image (true when <see cref="ContentId"/> is set).
    /// </summary>
    public bool IsInline => ContentId is not null;
}
