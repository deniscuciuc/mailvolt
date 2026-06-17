using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;

namespace MailVolt.Core;

/// <summary>
/// Builds an <see cref="EmailAttachment"/> using a fluent API.
/// </summary>
internal sealed class AttachmentBuilder : IAttachmentBuilder
{
    private static readonly Dictionary<string, string> KnownMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = "text/plain",
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "application/javascript",
        [".json"] = "application/json",
        [".xml"] = "application/xml",
        [".csv"] = "text/csv",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".zip"] = "application/zip",
        [".gz"] = "application/gzip",
        [".tar"] = "application/x-tar",
    };

    private string? _fileName;
    private Stream? _content;
    private string? _contentType;
    private bool _isContentTypeExplicit;
    private string? _contentId;

    /// <inheritdoc />
    public IAttachmentBuilder FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        _fileName ??= Path.GetFileName(path);
        _content = File.OpenRead(path);
        _contentType ??= DetectContentType(_fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder FromStream(string fileName, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(stream);

        _fileName ??= fileName;
        _content = stream;
        _contentType ??= DetectContentType(fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder FromBytes(string fileName, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(bytes);

        _fileName ??= fileName;
        _content = new MemoryStream(bytes);
        _contentType ??= DetectContentType(fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder AsInlineImage(string contentId)
    {
        ArgumentNullException.ThrowIfNull(contentId);

        _contentId = contentId;
        if (!_isContentTypeExplicit)
        {
            _contentType = "image/png";
        }

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder WithContentType(string contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        _contentType = contentType;
        _isContentTypeExplicit = true;
        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder WithFileName(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        _fileName = fileName;
        return this;
    }

    /// <summary>
    /// Builds the <see cref="EmailAttachment"/> from the accumulated state.
    /// </summary>
    /// <returns>The constructed <see cref="EmailAttachment"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="EmailAttachment.FileName"/> or <see cref="EmailAttachment.Content"/> is not set.</exception>
    internal EmailAttachment Build()
    {
        if (_fileName is null)
        {
            throw new InvalidOperationException("File name must be set before building the attachment.");
        }

        return _content is null
            ? throw new InvalidOperationException("Content must be set before building the attachment.")
            : new EmailAttachment
        {
            FileName = _fileName,
            Content = _content,
            ContentType = _contentType ?? "application/octet-stream",
            ContentId = _contentId,
        };
    }

    private static string DetectContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return extension is { Length: > 0 } && KnownMimeTypes.TryGetValue(extension, out var mime)
            ? mime
            : "application/octet-stream";
    }
}
