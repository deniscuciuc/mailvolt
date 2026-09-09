using MailKit.Net.Smtp;
using MailKit.Security;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MailVolt.Transport.Smtp;

public sealed class SmtpSender(IOptions<SmtpSenderOptions> options) : ISender
{
    private readonly SmtpSenderOptions _options = options.Value;

    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            using var client = new SmtpClient
            {
                Timeout = _options.TimeoutMs
            };
            using var message = BuildMimeMessage(email);

            await client.ConnectAsync(_options.Host, _options.Port, _options.Security, cancellationToken).ConfigureAwait(false);

            if (_options.OAuth2TokenProvider is not null)
            {
                var token = await _options.OAuth2TokenProvider(cancellationToken).ConfigureAwait(false);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(_options.Username ?? string.Empty, token),
                    cancellationToken).ConfigureAwait(false);
            }
            else if (_options.Username is not null && _options.Password is not null)
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken).ConfigureAwait(false);
            }

            var response = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            return EmailResult.Success(response);
        }
        // Cancellation propagates rather than becoming a send failure: every MailVolt
        // transport behaves the same way, so swapping providers does not change how a
        // cancelled send is observed.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EmailResult.Failure($"SMTP send failed: {ex.Message}", ex);
        }
    }

    private static MimeMessage BuildMimeMessage(EmailMessage email)
    {
        var message = new MimeMessage();

        if (email.From is not null)
            message.From.Add(new MailboxAddress(email.From.DisplayName, email.From.Address));

        foreach (var to in email.To)
            message.To.Add(new MailboxAddress(to.DisplayName, to.Address));

        foreach (var cc in email.Cc)
            message.Cc.Add(new MailboxAddress(cc.DisplayName, cc.Address));

        foreach (var bcc in email.Bcc)
            message.Bcc.Add(new MailboxAddress(bcc.DisplayName, bcc.Address));

        if (email.ReplyTo is not null)
            message.ReplyTo.Add(new MailboxAddress(email.ReplyTo.DisplayName, email.ReplyTo.Address));

        message.Subject = email.Subject;

        var body = new BodyBuilder();

        if (email.TextBody is not null)
            body.TextBody = email.TextBody;

        if (email.HtmlBody is not null)
            body.HtmlBody = email.HtmlBody;

        foreach (var attachment in email.Attachments)
        {
            if (attachment.IsInline)
            {
                var linked = body.LinkedResources.Add(attachment.FileName,
                    attachment.Content.ToArray(), ContentType.Parse(attachment.ContentType));
                linked.ContentId = attachment.ContentId;
            }
            else
            {
                body.Attachments.Add(attachment.FileName, attachment.Content.ToArray(),
                    ContentType.Parse(attachment.ContentType));
            }
        }

        message.Body = body.ToMessageBody();

        message.Headers["X-Priority"] = email.Priority switch
        {
            EmailPriority.Low => "5",
            EmailPriority.Normal => "3",
            EmailPriority.High => "1",
            _ => "3"
        };

        foreach (var header in email.Headers)
            message.Headers[header.Key] = header.Value;

        return message;
    }
}
