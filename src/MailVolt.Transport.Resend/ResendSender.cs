using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;
using EmailAddress = Resend.EmailAddress;
using ResendEmailAttachment = Resend.EmailAttachment;
using ResendEmailMessage = Resend.EmailMessage;
using ResendEmailTag = Resend.EmailTag;

namespace MailVolt.Transport.Resend;

/// <summary>
/// Sends emails via the Resend API using the official Resend .NET SDK.
/// </summary>
internal sealed class ResendSender : IResendSender
{
    private readonly global::Resend.IResend _resend;
    private readonly ILogger<ResendSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResendSender"/> class.
    /// </summary>
    /// <param name="resend">The Resend client.</param>
    /// <param name="logger">The logger.</param>
    public ResendSender(global::Resend.IResend resend, ILogger<ResendSender> logger)
    {
        ArgumentNullException.ThrowIfNull(resend);
        ArgumentNullException.ThrowIfNull(logger);

        _resend = resend;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var message = MapToEmailMessage(email);

        try
        {
            var response = await _resend.EmailSendAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.Success)
            {
                var messageId = response.Content == Guid.Empty
                    ? null
                    : response.Content.ToString();

                _logger.LogInformation("Email sent successfully via Resend. MessageId: {MessageId}", messageId);
                return EmailResult.Success(messageId);
            }

            var errorMessage = response.Exception?.Message ?? "Resend API returned an unsuccessful response.";
            _logger.LogError("Failed to send email via Resend. Error: {Error}", errorMessage);
            return EmailResult.Failure(errorMessage, response.Exception);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email sending via Resend was cancelled.");
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "An unexpected error occurred while sending email via Resend.");
            return EmailResult.Failure($"An unexpected error occurred: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Maps a MailVolt <see cref="EmailMessage"/> to a Resend <see cref="ResendEmailMessage"/>.
    /// </summary>
    internal static ResendEmailMessage MapToEmailMessage(EmailMessage email)
    {
        var message = new ResendEmailMessage
        {
            From = email.From?.ToString() ?? string.Empty,
            Subject = email.Subject,
        };

        var to = new global::Resend.EmailAddressList();
        to.AddRange(email.To.Select(address => address.ToString()).Select(dummy => (EmailAddress)dummy));

        message.To = to;

        if (email.Cc.Count > 0)
        {
            var cc = new global::Resend.EmailAddressList();
            cc.AddRange(email.Cc.Select(address => address.ToString()).Select(dummy => (EmailAddress)dummy));

            message.Cc = cc;
        }

        if (email.Bcc.Count > 0)
        {
            var bcc = new global::Resend.EmailAddressList();
            bcc.AddRange(email.Bcc.Select(address => address.ToString()).Select(dummy => (EmailAddress)dummy));

            message.Bcc = bcc;
        }

        if (email.ReplyTo is not null)
        {
            var replyTo = new global::Resend.EmailAddressList
            {
                email.ReplyTo.ToString()
            };
            message.ReplyTo = replyTo;
        }

        if (email.HtmlBody is { Length: > 0 })
        {
            message.HtmlBody = email.HtmlBody;
        }

        if (email.TextBody is { Length: > 0 })
        {
            message.TextBody = email.TextBody;
        }

        if (email.Headers.Count > 0)
        {
            message.Headers = new Dictionary<string, string>(email.Headers);
        }

        if (email.Tags.Count > 0)
        {
            message.Tags = [.. email.Tags.Select(tag => new ResendEmailTag { Name = tag, Value = tag })];
        }

        if (email.Attachments.Count <= 0) return message;

        var attachments = new List<ResendEmailAttachment>();
        foreach (var attachment in email.Attachments)
        {
            using var memoryStream = new MemoryStream();
            attachment.Content.CopyTo(memoryStream);

            attachments.Add(new ResendEmailAttachment
            {
                Filename = attachment.FileName,
                Content = Convert.ToBase64String(memoryStream.ToArray()),
                ContentType = attachment.ContentType,
                ContentId = attachment.ContentId,
            });
        }

        message.Attachments = attachments;

        return message;
    }
}
