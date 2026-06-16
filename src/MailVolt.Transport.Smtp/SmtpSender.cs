namespace MailVolt.Transport.Smtp;

using MailKit.Net.Smtp;
using MailKit.Security;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Options;
using MimeKit;

public sealed class SmtpSender : ISender
{
    private readonly SmtpSenderOptions _options;

    public SmtpSender(IOptions<SmtpSenderOptions> options)
    {
        _options = options.Value;
    }

    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();
            var message = BuildMimeMessage(email);

            await client.ConnectAsync(_options.Host, _options.Port, _options.Security, cancellationToken);

            if (_options.OAuth2TokenProvider is not null)
            {
                var token = await _options.OAuth2TokenProvider(cancellationToken);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(_options.Username ?? string.Empty, token), cancellationToken);
            }
            else if (_options.Username is not null && _options.Password is not null)
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            var response = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return EmailResult.Success(response);
        }
        catch (Exception ex)
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
                var linked = body.LinkedResources.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                linked.ContentId = attachment.ContentId;
            }
            else
            {
                body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }
        }

        message.Body = body.ToMessageBody();

        // Set priority
        message.Headers["X-Priority"] = email.Priority switch
        {
            EmailPriority.Low => "5",
            EmailPriority.Normal => "3",
            EmailPriority.High => "1",
            _ => "3"
        };

        // Add custom headers
        foreach (var header in email.Headers)
            message.Headers[header.Key] = header.Value;

        return message;
    }
}
