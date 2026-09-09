using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Options;
using Microsoft.Extensions.Options;

namespace MailVolt.Core;

/// <summary>
/// Fluent builder for constructing and sending email messages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EmailBuilder"/> class.
/// </remarks>
/// <param name="options">The MailVolt configuration options.</param>
/// <param name="templateRenderer">Optional template renderer for rendering template-based emails.</param>
/// <param name="sender">Optional sender for inline SendAsync operations.</param>
internal sealed class EmailBuilder(
    IOptions<MailVoltOptions> options,
    ITemplateRenderer? templateRenderer = null,
    ISender? sender = null) : IEmailBuilder
{
    private readonly MailVoltOptions _options = options.Value;

    private EmailAddress? _from;
    private readonly List<EmailAddress> _to = [];
    private readonly List<EmailAddress> _cc = [];
    private readonly List<EmailAddress> _bcc = [];
    private EmailAddress? _replyTo;
    private string _subject = string.Empty;
    private string? _textBody;
    private string? _htmlBody;
    private EmailPriority _priority = EmailPriority.Normal;
    private readonly List<string> _tags = [];
    private readonly Dictionary<string, string> _headers = [];
    private readonly List<AttachmentBuilder> _attachmentBuilders = [];
    private string? _template;
    private object? _templateModel;

    /// <inheritdoc />
    public IEmailBuilder From(EmailAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        _from = address;
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder To(EmailAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        _to.Add(address);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Cc(EmailAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        _cc.Add(address);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Bcc(EmailAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        _bcc.Add(address);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder ReplyTo(EmailAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        _replyTo = address;
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Subject(string subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Body(string text)
    {
        _textBody = text ?? throw new ArgumentNullException(nameof(text));
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder HtmlBody(string html)
    {
        _htmlBody = html ?? throw new ArgumentNullException(nameof(html));
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder TextBody(string text)
    {
        _textBody = text ?? throw new ArgumentNullException(nameof(text));
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Priority(EmailPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Tag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        _tags.Add(tag);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Header(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        _headers[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Attach(Action<IAttachmentBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var attachmentBuilder = new AttachmentBuilder();
        configure(attachmentBuilder);
        _attachmentBuilders.Add(attachmentBuilder);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder UsingTemplate<TModel>(string template, TModel model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        _template = template;
        _templateModel = model;
        return this;
    }

    /// <inheritdoc />
    public async Task<EmailMessage> BuildAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_to.Count == 0)
        {
            throw new InvalidOperationException("At least one recipient (To) is required.");
        }

        if (string.IsNullOrWhiteSpace(_subject))
        {
            throw new InvalidOperationException("Subject is required.");
        }

        // Resolved into a local rather than assigned to _from, so BuildAsync stays free
        // of side effects and can be called more than once.
        var from = _from;
        if (from is null)
        {
            if (_options.DefaultFromAddress is { Length: > 0 } defaultFrom)
            {
                from = new EmailAddress(defaultFrom, _options.DefaultFromDisplayName);
            }
            else
            {
                throw new InvalidOperationException(
                    "A From address must be specified or MailVoltOptions.DefaultFromAddress must be configured.");
            }
        }

        var htmlBody = _htmlBody;
        if (_template is not null && htmlBody is null && _textBody is null)
        {
            // Previously an unrenderable template was skipped in silence and an email with
            // an empty body was sent. Failing loudly is the only safe behaviour here.
            if (templateRenderer is null)
            {
                throw new InvalidOperationException(
                    $"Template '{_template}' cannot be rendered because no ITemplateRenderer is registered. " +
                    "Register one with UseRazorTemplates(), UseLiquidTemplates() or UseHandlebarsTemplates().");
            }

            if (_templateModel is null)
            {
                throw new InvalidOperationException(
                    $"Template '{_template}' was specified without a model. Pass a model to UsingTemplate.");
            }

            htmlBody = await templateRenderer
                .RenderAsync(_template, _templateModel, cancellationToken)
                .ConfigureAwait(false);
        }

        var attachments = new List<EmailAttachment>(_attachmentBuilders.Count);
        foreach (var attachmentBuilder in _attachmentBuilders)
        {
            attachments.Add(await attachmentBuilder.BuildAsync(cancellationToken).ConfigureAwait(false));
        }

        // Every collection is copied, not wrapped. List<T>.AsReadOnly() returns a view over
        // the live list, so a reused builder would mutate messages it had already produced.
        return new EmailMessage
        {
            From = from,
            To = [.. _to],
            Cc = [.. _cc],
            Bcc = [.. _bcc],
            ReplyTo = _replyTo,
            Subject = _subject,
            TextBody = _textBody,
            HtmlBody = htmlBody,
            Priority = _priority,
            Attachments = attachments,
            Headers = new Dictionary<string, string>(_headers),
            Tags = [.. _tags],
        };
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(CancellationToken cancellationToken = default)
    {
        if (sender is null)
        {
            throw new InvalidOperationException(
                "An ISender must be registered in the DI container to use SendAsync. " +
                "Alternatively, use BuildAsync to construct the message and send it manually.");
        }

        var email = await BuildAsync(cancellationToken).ConfigureAwait(false);
        return await sender.SendAsync(email, cancellationToken).ConfigureAwait(false);
    }
}
