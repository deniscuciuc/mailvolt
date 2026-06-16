using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailVolt.Transport.SendGrid;

/// <summary>
/// Sends email messages via the SendGrid v3 <c>/v3/mail/send</c> API.
/// </summary>
internal sealed partial class SendGridSender : ISendGridSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly SendGridSenderOptions _options;
    private readonly ILogger<SendGridSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendGridSender"/> class.
    /// </summary>
    /// <param name="httpClient">The typed <see cref="HttpClient"/> configured for SendGrid.</param>
    /// <param name="options">The SendGrid sender options.</param>
    /// <param name="logger">The logger instance.</param>
    public SendGridSender(
        HttpClient httpClient,
        IOptions<SendGridSenderOptions> options,
        ILogger<SendGridSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Ensure base address is set from options if not already configured.
        _httpClient.BaseAddress ??= new Uri(_options.BaseUrl);
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = BuildPayload(email);

            using var response = await _httpClient.PostAsJsonAsync(
                "/v3/mail/send",
                payload,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                LogSendGridError(response.StatusCode, errorBody);
                return EmailResult.Failure($"SendGrid returned {(int)response.StatusCode}: {errorBody}");
            }

            var messageId = response.Headers.TryGetValues("x-message-id", out var values)
                ? values.FirstOrDefault()
                : null;

            LogSendGridSuccess(messageId);
            return EmailResult.Success(messageId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogSendGridException(ex);
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Builds the SendGrid v3 JSON payload from the email message.
    /// </summary>
    private object BuildPayload(EmailMessage email)
    {
        var from = MapAddress(email.From)
            ?? throw new InvalidOperationException("A From address is required.");

        var personalization = new SendGridPersonalization(
            To: email.To.Select(MapAddress).ToList(),
            Cc: email.Cc.Count > 0 ? email.Cc.Select(MapAddress).ToList() : null,
            Bcc: email.Bcc.Count > 0 ? email.Bcc.Select(MapAddress).ToList() : null,
            Subject: !_options.UseDynamicTemplates ? email.Subject : null,
            Headers: email.Headers.Count > 0 ? new Dictionary<string, string>(email.Headers) : null);

        // Build content array — omitted when using dynamic templates without explicit body content.
        List<SendGridContent>? content = null;
        if (!_options.UseDynamicTemplates || email.TextBody is not null || email.HtmlBody is not null)
        {
            content = [];
            if (email.TextBody is { Length: > 0 } text)
            {
                content.Add(new SendGridContent("text/plain", text));
            }

            if (email.HtmlBody is { Length: > 0 } html)
            {
                content.Add(new SendGridContent("text/html", html));
            }
        }

        // Build attachments.
        List<SendGridAttachment>? attachments = null;
        if (email.Attachments.Count > 0)
        {
            attachments = [];
            foreach (var attachment in email.Attachments)
            {
                attachments.Add(MapAttachment(attachment));
            }
        }

        // Build categories from tags.
        List<string>? categories = email.Tags.Count > 0 ? [.. email.Tags] : null;

        // Build mail_settings / sandbox_mode.
        SendGridMailSettings? mailSettings = null;
        if (_options.SandboxMode is { Length: > 0 } sandboxStr
            && bool.TryParse(sandboxStr, out var sandboxEnabled))
        {
            mailSettings = new SendGridMailSettings(new SendGridSandboxMode(sandboxEnabled));
        }

        return new SendGridPayload(
            Personalizations: [personalization],
            From: from,
            ReplyTo: email.ReplyTo is not null ? MapAddress(email.ReplyTo) : null,
            Subject: _options.UseDynamicTemplates ? null : email.Subject,
            Content: content,
            Attachments: attachments,
            Headers: email.Headers.Count > 0 ? new Dictionary<string, string>(email.Headers) : null,
            Categories: categories,
            MailSettings: mailSettings);
    }

    private static SendGridEmailAddress MapAddress(EmailAddress? address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new SendGridEmailAddress(address.Address, address.DisplayName);
    }

    private static SendGridAttachment MapAttachment(EmailAttachment attachment)
    {
        using var memoryStream = new MemoryStream();
        attachment.Content.CopyTo(memoryStream);
        var base64Content = Convert.ToBase64String(memoryStream.ToArray());

        return new SendGridAttachment(
            Content: base64Content,
            Filename: attachment.FileName,
            Type: attachment.ContentType,
            Disposition: attachment.IsInline ? "inline" : "attachment",
            ContentId: attachment.ContentId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "SendGrid API returned {StatusCode}")]
    private partial void LogSendGridError(System.Net.HttpStatusCode statusCode, string errorBody);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "SendGrid email accepted. MessageId: {MessageId}")]
    private partial void LogSendGridSuccess(string? messageId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "SendGrid sender threw an exception")]
    private partial void LogSendGridException(Exception exception);
}

// ──────────────────────────────────────────────
// SendGrid v3 JSON payload DTOs
// ──────────────────────────────────────────────

#pragma warning disable CA1812 // instantiated via System.Text.Json

/// <summary>
/// The root SendGrid v3 mail/send payload.
/// </summary>
internal sealed record SendGridPayload(
    [property: JsonPropertyName("personalizations")] List<SendGridPersonalization> Personalizations,
    [property: JsonPropertyName("from")] SendGridEmailAddress From,
    [property: JsonPropertyName("reply_to")] SendGridEmailAddress? ReplyTo,
    [property: JsonPropertyName("subject")] string? Subject,
    [property: JsonPropertyName("content")] List<SendGridContent>? Content,
    [property: JsonPropertyName("attachments")] List<SendGridAttachment>? Attachments,
    [property: JsonPropertyName("headers")] Dictionary<string, string>? Headers,
    [property: JsonPropertyName("categories")] List<string>? Categories,
    [property: JsonPropertyName("mail_settings")] SendGridMailSettings? MailSettings);

/// <summary>
/// A single personalization block containing recipients and per-email overrides.
/// </summary>
internal sealed record SendGridPersonalization(
    [property: JsonPropertyName("to")] List<SendGridEmailAddress> To,
    [property: JsonPropertyName("cc")] List<SendGridEmailAddress>? Cc,
    [property: JsonPropertyName("bcc")] List<SendGridEmailAddress>? Bcc,
    [property: JsonPropertyName("subject")] string? Subject,
    [property: JsonPropertyName("headers")] Dictionary<string, string>? Headers);

/// <summary>
/// An email address with an optional display name.
/// </summary>
internal sealed record SendGridEmailAddress(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name")] string? Name);

/// <summary>
/// A content MIME part.
/// </summary>
internal sealed record SendGridContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value);

/// <summary>
/// A file attachment.
/// </summary>
internal sealed record SendGridAttachment(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("disposition")] string? Disposition,
    [property: JsonPropertyName("content_id")] string? ContentId);

/// <summary>
/// Mail settings wrapper.
/// </summary>
internal sealed record SendGridMailSettings(
    [property: JsonPropertyName("sandbox_mode")] SendGridSandboxMode SandboxMode);

/// <summary>
/// Sandbox mode setting.
/// </summary>
internal sealed record SendGridSandboxMode(
    [property: JsonPropertyName("enable")] bool Enable);
