namespace MailVolt.Core.Models;

/// <summary>
/// Represents a file attached to an email message.
/// </summary>
/// <remarks>
/// The content is held as bytes rather than a <see cref="Stream"/> so that an
/// <see cref="EmailMessage"/> stays immutable and can be sent more than once — a
/// stream is consumed by the first send, which would silently produce an empty
/// attachment on a retry.
/// </remarks>
public sealed class EmailAttachment
{
    /// <summary>
    /// The file name of the attachment (e.g. "report.pdf").
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The content of the attachment.
    /// </summary>
    public required ReadOnlyMemory<byte> Content { get; init; }

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

    /// <summary>
    /// Opens a read-only <see cref="Stream"/> over <see cref="Content"/>. The caller owns
    /// the returned stream. A fresh stream is returned on every call, so the attachment
    /// can be sent repeatedly.
    /// </summary>
    public Stream OpenReadStream() => Content.AsStream();
}

internal static class ReadOnlyMemoryExtensions
{
    internal static Stream AsStream(this ReadOnlyMemory<byte> content) =>
        System.Runtime.InteropServices.MemoryMarshal.TryGetArray(content, out var segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(content.ToArray(), writable: false);
}
