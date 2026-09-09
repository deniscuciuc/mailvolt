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
    // Exactly one source is set. File reads and stream copies are deferred to
    // BuildAsync so the IO happens asynchronously, on the async build path, rather
    // than while the caller is still describing the attachment.
    private ReadOnlyMemory<byte>? _bytes;
    private string? _path;
    private Stream? _stream;
    private string? _contentType;
    private bool _isContentTypeExplicit;
    private string? _contentId;

    /// <inheritdoc />
    public IAttachmentBuilder FromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        _fileName ??= Path.GetFileName(path);
        ClearSources();
        _path = path;
        _contentType ??= DetectContentType(_fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder FromStream(string fileName, Stream stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(stream);

        _fileName ??= fileName;
        ClearSources();
        _stream = stream;
        _contentType ??= DetectContentType(fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder FromBytes(string fileName, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(bytes);

        _fileName ??= fileName;
        ClearSources();
        _bytes = bytes;
        _contentType ??= DetectContentType(fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder FromMemory(string fileName, ReadOnlyMemory<byte> content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        _fileName ??= fileName;
        ClearSources();
        _bytes = content;
        _contentType ??= DetectContentType(fileName);

        return this;
    }

    /// <inheritdoc />
    public IAttachmentBuilder AsInlineImage(string contentId)
    {
        ArgumentNullException.ThrowIfNull(contentId);

        _contentId = contentId;

        // Only fall back to PNG when the type could not be detected at all. Previously
        // this overwrote a correctly sniffed type, so .FromFile("logo.jpg") followed by
        // .AsInlineImage(...) declared a JPEG as image/png.
        if (!_isContentTypeExplicit &&
            (_contentType is null || _contentType == "application/octet-stream"))
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
    /// Materializes the <see cref="EmailAttachment"/>, reading the configured file or
    /// stream into memory.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The constructed <see cref="EmailAttachment"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the file name or content source is not set.</exception>
    internal async Task<EmailAttachment> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (_fileName is null)
        {
            throw new InvalidOperationException("File name must be set before building the attachment.");
        }

        var content = await ReadContentAsync(cancellationToken).ConfigureAwait(false);

        return new EmailAttachment
        {
            FileName = _fileName,
            Content = content,
            ContentType = _contentType ?? "application/octet-stream",
            ContentId = _contentId,
        };
    }

    private async Task<ReadOnlyMemory<byte>> ReadContentAsync(CancellationToken cancellationToken)
    {
        if (_bytes is { } bytes)
        {
            return bytes;
        }

        if (_path is { } path)
        {
            return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        }

        if (_stream is null)
        {
            throw new InvalidOperationException("Content must be set before building the attachment.");
        }

        // The caller owns the stream, so it is read but never disposed here.
        using var buffer = new MemoryStream();
        await _stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private void ClearSources()
    {
        _bytes = null;
        _path = null;
        _stream = null;
    }

    private static string DetectContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return extension is { Length: > 0 } && KnownMimeTypes.TryGetValue(extension, out var mime)
            ? mime
            : "application/octet-stream";
    }
}
