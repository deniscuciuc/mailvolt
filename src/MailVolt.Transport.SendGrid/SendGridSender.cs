using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGridEmailAddress = SendGrid.Helpers.Mail.EmailAddress;

namespace MailVolt.Transport.SendGrid;

/// <summary>
/// Sends email messages via the official Twilio SendGrid C# SDK.
/// </summary>
internal sealed partial class SendGridSender : ISendGridSender
{
    private readonly ISendGridClient _client;
    private readonly SendGridSenderOptions _options;
    private readonly ILogger<SendGridSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendGridSender"/> class.
    /// </summary>
    /// <param name="client">The SendGrid client used to send email.</param>
    /// <param name="options">The SendGrid sender options.</param>
    /// <param name="logger">The logger instance.</param>
    public SendGridSender(
        ISendGridClient client,
        IOptions<SendGridSenderOptions> options,
        ILogger<SendGridSender> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            var message = MapToSendGridMessage(email, _options);
            var response = await _client.SendEmailAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                    ? values.FirstOrDefault()
                    : null;

                LogSendGridSuccess(messageId);
                return EmailResult.Success(messageId);
            }

            var errorBody = response.Body is not null
                ? await response.Body.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
                : null;

            LogSendGridError(response.StatusCode, errorBody);
            return EmailResult.Failure($"SendGrid returned {(int)response.StatusCode}: {errorBody}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email sending via SendGrid was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            LogSendGridException(ex);
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Maps a MailVolt <see cref="EmailMessage"/> to a SendGrid <see cref="SendGridMessage"/>.
    /// </summary>
    internal static SendGridMessage MapToSendGridMessage(EmailMessage email, SendGridSenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(options);

        var message = new SendGridMessage
        {
            From = MapAddress(email.From),
            Subject = options.UseDynamicTemplates ? null : email.Subject,
        };

        foreach (var to in email.To)
        {
            message.AddTo(to.Address, to.DisplayName);
        }

        foreach (var cc in email.Cc)
        {
            message.AddCc(cc.Address, cc.DisplayName);
        }

        foreach (var bcc in email.Bcc)
        {
            message.AddBcc(bcc.Address, bcc.DisplayName);
        }

        if (email.ReplyTo is not null)
        {
            message.SetReplyTo(MapAddress(email.ReplyTo));
        }

        if (!options.UseDynamicTemplates || !string.IsNullOrEmpty(email.TextBody) ||
            !string.IsNullOrEmpty(email.HtmlBody))
        {
            if (!string.IsNullOrEmpty(email.TextBody))
            {
                message.AddContent("text/plain", email.TextBody);
            }

            if (!string.IsNullOrEmpty(email.HtmlBody))
            {
                message.AddContent("text/html", email.HtmlBody);
            }
        }

        if (email.Headers.Count > 0)
        {
            foreach (var header in email.Headers)
            {
                message.AddGlobalHeader(header.Key, header.Value);
            }
        }

        if (email.Tags.Count > 0)
        {
            message.AddCategories(email.Tags.ToList());
        }

        if (email.Attachments.Count > 0)
        {
            foreach (var attachment in email.Attachments)
            {
                using var memoryStream = new MemoryStream();
                attachment.Content.CopyTo(memoryStream);
                var base64Content = Convert.ToBase64String(memoryStream.ToArray());

                message.AddAttachment(
                    attachment.FileName,
                    base64Content,
                    attachment.ContentType,
                    attachment.IsInline ? "inline" : "attachment",
                    attachment.ContentId);
            }
        }

        if (options.SandboxMode is { Length: > 0 } sandboxValue
            && bool.TryParse(sandboxValue, out var sandboxEnabled))
        {
            message.SetSandBoxMode(sandboxEnabled);
        }

        return message;
    }

    private static SendGridEmailAddress MapAddress(Core.Models.EmailAddress? address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new SendGridEmailAddress(address.Address, address.DisplayName);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "SendGrid API returned {StatusCode}: {ErrorBody}")]
    private partial void LogSendGridError(System.Net.HttpStatusCode statusCode, string? errorBody);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "SendGrid email accepted. MessageId: {MessageId}")]
    private partial void LogSendGridSuccess(string? messageId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "SendGrid sender threw an exception")]
    private partial void LogSendGridException(Exception exception);
}
