using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Options;
using Microsoft.Extensions.Options;

namespace MailVolt.Core;

/// <summary>
/// Fluent builder for constructing and sending email messages.
/// </summary>
internal sealed class EmailBuilder : IEmailBuilder
{
    private readonly ITemplateRenderer? _templateRenderer;
    private readonly ISender? _sender;
    private readonly MailVoltOptions _options;

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
    private readonly List<EmailAttachment> _attachments = [];
    private string? _template;
    private object? _templateModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailBuilder"/> class.
    /// </summary>
    /// <param name="options">The MailVolt configuration options.</param>
    /// <param name="templateRenderer">Optional template renderer for rendering template-based emails.</param>
    /// <param name="sender">Optional sender for inline SendAsync operations.</param>
    public EmailBuilder(
        IOptions<MailVoltOptions> options,
        ITemplateRenderer? templateRenderer = null,
        ISender? sender = null)
    {
        _options = options.Value;
        _templateRenderer = templateRenderer;
        _sender = sender;
    }

    /// <inheritdoc />
    public IEmailBuilder From(EmailAddress address)
    {
        _from = address;
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder To(EmailAddress address)
    {
        _to.Add(address);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Cc(EmailAddress address)
    {
        _cc.Add(address);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Bcc(EmailAddress address)
    {
        _bcc.Add(address);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder ReplyTo(EmailAddress address)
    {
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
        _tags.Add(tag);
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Header(string key, string value)
    {
        _headers[key] = value;
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder Attach(Action<IAttachmentBuilder> configure)
    {
        var attachmentBuilder = new AttachmentBuilder();
        configure(attachmentBuilder);
        _attachments.Add(((AttachmentBuilder)attachmentBuilder).Build());
        return this;
    }

    /// <inheritdoc />
    public IEmailBuilder UsingTemplate<TModel>(string template, TModel model)
    {
        _template = template ?? throw new ArgumentNullException(nameof(template));
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

        // Apply default From address if not explicitly set.
        if (_from is null)
        {
            if (_options.DefaultFromAddress is { Length: > 0 } defaultFrom)
            {
                _from = new EmailAddress(defaultFrom, _options.DefaultFromDisplayName);
            }
            else
            {
                throw new InvalidOperationException(
                    "A From address must be specified or MailVoltOptions.DefaultFromAddress must be configured.");
            }
        }

        // Render template if configured.
        if (_template is not null && _templateModel is not null && _templateRenderer is not null)
        {
            var rendered = await _templateRenderer.RenderAsync(_template, _templateModel, cancellationToken);

            // If no explicit body was set, assume the template output is HTML.
            if (_htmlBody is null && _textBody is null)
            {
                _htmlBody = rendered;
            }
        }

        return new EmailMessage
        {
            From = _from,
            To = _to.AsReadOnly(),
            Cc = _cc.AsReadOnly(),
            Bcc = _bcc.AsReadOnly(),
            ReplyTo = _replyTo,
            Subject = _subject,
            TextBody = _textBody,
            HtmlBody = _htmlBody,
            Priority = _priority,
            Attachments = _attachments.AsReadOnly(),
            Headers = new Dictionary<string, string>(_headers),
            Tags = _tags.AsReadOnly(),
        };
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(CancellationToken cancellationToken = default)
    {
        if (_sender is null)
        {
            throw new InvalidOperationException(
                "An ISender must be registered in the DI container to use SendAsync. " +
                "Alternatively, use BuildAsync to construct the message and send it manually.");
        }

        var email = await BuildAsync(cancellationToken);
        return await _sender.SendAsync(email, cancellationToken);
    }
}
