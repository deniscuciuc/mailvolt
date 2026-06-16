using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Options;

namespace MailVolt.Transport.AzureEmail;

/// <summary>
/// Sends email via Azure Email Communication Services.
/// </summary>
public sealed class AzureEmailSender : ISender
{
    private readonly Azure.Communication.Email.EmailClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEmailSender"/> class.
    /// </summary>
    /// <param name="options">The Azure sender options containing the connection string.</param>
    public AzureEmailSender(IOptions<AzureEmailSenderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _client = new Azure.Communication.Email.EmailClient(options.Value.ConnectionString);
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            var senderAddress = email.From?.Address
                ?? throw new InvalidOperationException("The From address is required when sending via Azure Email.");

            var recipients = new Azure.Communication.Email.EmailRecipients(
                MapAddresses(email.To),
                MapAddresses(email.Cc),
                MapAddresses(email.Bcc));

            var content = new Azure.Communication.Email.EmailContent(email.Subject);

            if (email.HtmlBody is { Length: > 0 } html)
            {
                content.Html = html;
            }

            if (email.TextBody is { Length: > 0 } text)
            {
                content.PlainText = text;
            }

            var attachments = new List<Azure.Communication.Email.EmailAttachment>(email.Attachments.Count);

            foreach (var attachment in email.Attachments)
            {
                await using var ms = new MemoryStream();
                await attachment.Content.CopyToAsync(ms, cancellationToken);
                var binaryData = System.BinaryData.FromBytes(ms.ToArray());

                var azureAttachment = new Azure.Communication.Email.EmailAttachment(
                    attachment.FileName,
                    attachment.ContentType,
                    binaryData);

                attachments.Add(azureAttachment);
            }

            var azureMessage = new Azure.Communication.Email.EmailMessage(senderAddress, recipients, content);

            foreach (var azureAttachment in attachments)
            {
                azureMessage.Attachments.Add(azureAttachment);
            }

            foreach (var header in email.Headers)
            {
                azureMessage.Headers[header.Key] = header.Value;
            }

            var operation = await _client.SendAsync(
                Azure.WaitUntil.Completed,
                azureMessage,
                cancellationToken);

            return EmailResult.Success(operation.Id);
        }
        catch (Exception ex)
        {
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    private static List<Azure.Communication.Email.EmailAddress> MapAddresses(IReadOnlyList<EmailAddress> addresses)
    {
        var result = new List<Azure.Communication.Email.EmailAddress>(addresses.Count);

        foreach (var addr in addresses)
        {
            result.Add(new Azure.Communication.Email.EmailAddress(addr.Address, addr.DisplayName));
        }

        return result;
    }
}
