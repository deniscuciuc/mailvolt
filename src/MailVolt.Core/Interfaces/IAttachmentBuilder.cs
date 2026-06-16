namespace MailVolt.Core.Interfaces;

/// <summary>
/// Fluent builder for constructing an <see cref="MailVolt.Core.Models.EmailAttachment"/>.
/// </summary>
public interface IAttachmentBuilder
{
    /// <summary>
    /// Loads attachment content from a file on disk.
    /// </summary>
    /// <param name="path">The full file path.</param>
    /// <returns>The builder instance for chaining.</returns>
    IAttachmentBuilder FromFile(string path);

    /// <summary>
    /// Loads attachment content from a stream.
    /// </summary>
    /// <param name="fileName">The file name for the attachment.</param>
    /// <param name="stream">The content stream.</param>
    /// <returns>The builder instance for chaining.</returns>
    IAttachmentBuilder FromStream(string fileName, Stream stream);

    /// <summary>
    /// Loads attachment content from a byte array.
    /// </summary>
    /// <param name="fileName">The file name for the attachment.</param>
    /// <param name="bytes">The content bytes.</param>
    /// <returns>The builder instance for chaining.</returns>
    IAttachmentBuilder FromBytes(string fileName, byte[] bytes);

    /// <summary>
    /// Marks the attachment as an inline image with the specified content identifier.
    /// </summary>
    /// <param name="contentId">The content ID (e.g. "logo@mailvolt").</param>
    /// <returns>The builder instance for chaining.</returns>
    IAttachmentBuilder AsInlineImage(string contentId);

    /// <summary>
    /// Overrides the MIME content type.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <returns>The builder instance for chaining.</returns>
    IAttachmentBuilder WithContentType(string contentType);

    /// <summary>
    /// Overrides the file name.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The builder instance for chaining.</returns>
    IAttachmentBuilder WithFileName(string fileName);
}
