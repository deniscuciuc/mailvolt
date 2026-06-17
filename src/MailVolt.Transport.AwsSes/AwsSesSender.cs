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
/// <remarks>
/// Initializes a new instance of the <see cref="AwsSesSender"/> class.
/// </remarks>
/// <param name="options">The AWS SES options.</param>
public sealed class AwsSesSender(IOptions<AwsSesSenderOptions> options) : ISender
{
    private readonly AwsSesSenderOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        try
        {
            var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
            var regionEndpoint = RegionEndpoint.GetBySystemName(_options.Region);

            using var client = new AmazonSimpleEmailServiceV2Client(credentials, regionEndpoint);

            return email.Attachments.Count > 0
                ? await SendWithAttachmentsAsync(client, email, cancellationToken)
                : await SendSimpleAsync(client, email, cancellationToken);
        }
        catch (Exception ex)
        {
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    private async Task<EmailResult> SendSimpleAsync(
        AmazonSimpleEmailServiceV2Client client,
        EmailMessage email,
        CancellationToken ct)
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
            ConfigurationSetName = _options.ConfigurationSetName
        };

        var response = await client.SendEmailAsync(request, ct);
        return EmailResult.Success(response.MessageId);
    }

    private async Task<EmailResult> SendWithAttachmentsAsync(
        AmazonSimpleEmailServiceV2Client client,
        EmailMessage email,
        CancellationToken ct)
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

        mimeMessage.Subject = email.Subject;

        foreach (var header in email.Headers)
            mimeMessage.Headers.Add(header.Key, header.Value);

        var bodyBuilder = new BodyBuilder();

        if (email.TextBody is not null)
            bodyBuilder.TextBody = email.TextBody;

        if (email.HtmlBody is not null)
            bodyBuilder.HtmlBody = email.HtmlBody;

        foreach (var attachment in email.Attachments)
        {
            await bodyBuilder.Attachments.AddAsync(attachment.FileName, attachment.Content,
                ContentType.Parse(attachment.ContentType), ct);
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        await using var memoryStream = new MemoryStream();
        await mimeMessage.WriteToAsync(memoryStream, ct);
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

        var response = await client.SendEmailAsync(request, ct);
        return EmailResult.Success(response.MessageId);
    }
}
