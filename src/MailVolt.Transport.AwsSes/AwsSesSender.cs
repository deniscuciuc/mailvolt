using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MailVolt.Transport.AwsSes;

/// <summary>
/// Sends email messages via the AWS SES v2 API.
/// </summary>
public sealed class AwsSesSender : ISender, IDisposable
{
    private readonly AwsSesSenderOptions _options;

    // Built once rather than per send: the SES client owns an HTTP connection pool, so
    // constructing one per email discarded every pooled connection.
    private readonly AmazonSimpleEmailServiceV2Client _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsSesSender"/> class.
    /// </summary>
    /// <param name="options">The AWS SES options.</param>
    public AwsSesSender(IOptions<AwsSesSenderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _client = new AmazonSimpleEmailServiceV2Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            RegionEndpoint.GetBySystemName(_options.Region));
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            return email.Attachments.Count > 0
                ? await SendWithAttachmentsAsync(_client, email, cancellationToken).ConfigureAwait(false)
                : await SendSimpleAsync(_client, email, cancellationToken).ConfigureAwait(false);
        }
        // Cancellation propagates rather than becoming a send failure: every MailVolt
        // transport behaves the same way, so swapping providers does not change how a
        // cancelled send is observed.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    private async Task<EmailResult> SendSimpleAsync(
        AmazonSimpleEmailServiceV2Client client,
        EmailMessage email,
        CancellationToken ct)
    {
        var request = BuildSimpleRequest(email, _options.ConfigurationSetName);

        var response = await client.SendEmailAsync(request, ct).ConfigureAwait(false);
        return EmailResult.Success(response.MessageId);
    }

    /// <summary>
    /// Maps an <see cref="EmailMessage"/> with no attachments onto an SES structured request.
    /// </summary>
    /// <remarks>Internal so the mapping can be unit tested without calling AWS.</remarks>
    internal static SendEmailRequest BuildSimpleRequest(EmailMessage email, string? configurationSetName)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = email.From?.ToString(),
            Destination = new Destination
            {
                ToAddresses = [.. email.To.Select(static t => t.ToString())],
                CcAddresses = [.. email.Cc.Select(static c => c.ToString())],
                BccAddresses = [.. email.Bcc.Select(static b => b.ToString())]
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = email.Subject },
                    Body = new Body
                    {
                        Text = email.TextBody is not null ? new Content { Data = email.TextBody } : null,
                        Html = email.HtmlBody is not null ? new Content { Data = email.HtmlBody } : null
                    }
                }
            },
            ConfigurationSetName = configurationSetName
        };

        // SES's structured request has no Reply-To field, so it goes on the request instead
        // of being dropped as it previously was.
        if (email.ReplyTo is not null)
        {
            request.ReplyToAddresses = [email.ReplyTo.ToString()];
        }

        return request;
    }

    private async Task<EmailResult> SendWithAttachmentsAsync(
        AmazonSimpleEmailServiceV2Client client,
        EmailMessage email,
        CancellationToken ct)
    {
        using var mimeMessage = BuildMimeMessage(email);

        var memoryStream = new MemoryStream();
        await using var streamScope = memoryStream.ConfigureAwait(false);
        await mimeMessage.WriteToAsync(memoryStream, ct).ConfigureAwait(false);
        memoryStream.Position = 0;

        var request = new SendEmailRequest
        {
            FromEmailAddress = email.From?.ToString(),
            Destination = new Destination
            {
                ToAddresses = [.. email.To.Select(static t => t.ToString())],
                CcAddresses = [.. email.Cc.Select(static c => c.ToString())],
                BccAddresses = [.. email.Bcc.Select(static b => b.ToString())]
            },
            Content = new EmailContent
            {
                Raw = new RawMessage { Data = memoryStream }
            },
            ConfigurationSetName = _options.ConfigurationSetName
        };

        var response = await client.SendEmailAsync(request, ct).ConfigureAwait(false);
        return EmailResult.Success(response.MessageId);
    }

    /// <summary>
    /// Builds the raw MIME message SES sends when the email has attachments.
    /// </summary>
    /// <remarks>Internal so the mapping can be unit tested without calling AWS.</remarks>
    internal static MimeMessage BuildMimeMessage(EmailMessage email)
    {
        var mimeMessage = new MimeMessage();

        if (email.From is not null)
            mimeMessage.From.Add(new MailboxAddress(email.From.DisplayName, email.From.Address));

        foreach (var to in email.To)
            mimeMessage.To.Add(new MailboxAddress(to.DisplayName, to.Address));
        foreach (var cc in email.Cc)
            mimeMessage.Cc.Add(new MailboxAddress(cc.DisplayName, cc.Address));
        foreach (var bcc in email.Bcc)
            mimeMessage.Bcc.Add(new MailboxAddress(bcc.DisplayName, bcc.Address));

        // Previously dropped on this path, so a Reply-To was silently lost whenever the
        // email had an attachment.
        if (email.ReplyTo is not null)
            mimeMessage.ReplyTo.Add(new MailboxAddress(email.ReplyTo.DisplayName, email.ReplyTo.Address));

        mimeMessage.Subject = email.Subject;

        var bodyBuilder = new BodyBuilder();

        if (email.TextBody is not null)
            bodyBuilder.TextBody = email.TextBody;

        if (email.HtmlBody is not null)
            bodyBuilder.HtmlBody = email.HtmlBody;

        foreach (var attachment in email.Attachments)
        {
            // Inline images were previously added as ordinary attachments, so a cid:
            // reference in the HTML never resolved and the image showed up as a download.
            if (attachment.IsInline)
            {
                var linked = bodyBuilder.LinkedResources.Add(attachment.FileName,
                    attachment.Content.ToArray(), ContentType.Parse(attachment.ContentType));
                linked.ContentId = attachment.ContentId;
            }
            else
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content.ToArray(),
                    ContentType.Parse(attachment.ContentType));
            }
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        mimeMessage.Headers["X-Priority"] = email.Priority switch
        {
            EmailPriority.Low => "5",
            EmailPriority.Normal => "3",
            EmailPriority.High => "1",
            _ => "3"
        };

        foreach (var header in email.Headers)
            mimeMessage.Headers[header.Key] = header.Value;

        return mimeMessage;
    }
}
