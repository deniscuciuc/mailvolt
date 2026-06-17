using MailVolt.Core.Models;
using Microsoft.Extensions.Options;
using brevo_csharp.Api;
using brevo_csharp.Model;
using ClientConfiguration = brevo_csharp.Client.Configuration;

namespace MailVolt.Transport.Brevo;

/// <summary>
/// Sends email via the Brevo (formerly Sendinblue) API using the official Brevo C# SDK.
/// </summary>
public sealed class BrevoSender : IBrevoSender
{
    private readonly ITransactionalEmailsApi _api;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrevoSender"/> class.
    /// </summary>
    /// <param name="options">The Brevo sender options containing the API key.</param>
    public BrevoSender(IOptions<BrevoSenderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var configuration = new ClientConfiguration();
        configuration.AddApiKey("api-key", options.Value.ApiKey);
        _api = new TransactionalEmailsApi(configuration);
    }

    internal BrevoSender(ITransactionalEmailsApi api)
    {
        _api = api;
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = BuildSendSmtpEmail(email);
            var result = await _api.SendTransacEmailAsync(request).ConfigureAwait(false);

            return EmailResult.Success(result?.MessageId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    internal static SendSmtpEmail BuildSendSmtpEmail(EmailMessage email)
    {
        var sender = email.From is not null
            ? new SendSmtpEmailSender(email.From.DisplayName, email.From.Address)
            : null;

        var replyTo = email.ReplyTo is not null
            ? new SendSmtpEmailReplyTo(email.ReplyTo.Address, email.ReplyTo.DisplayName)
            : null;

        var attachments = new List<SendSmtpEmailAttachment>(email.Attachments.Count);

        foreach (var attachment in email.Attachments)
        {
            using var ms = new MemoryStream();
            attachment.Content.CopyTo(ms);
            attachments.Add(new SendSmtpEmailAttachment(null, ms.ToArray(), attachment.FileName));
        }

        return new SendSmtpEmail(
            sender: sender,
            to: MapTo(email.To),
            bcc: MapBcc(email.Bcc),
            cc: MapCc(email.Cc),
            htmlContent: email.HtmlBody,
            textContent: email.TextBody,
            subject: email.Subject,
            replyTo: replyTo,
            attachment: attachments.Count > 0 ? attachments : null,
            headers: email.Headers.Count > 0 ? email.Headers : null,
            templateId: null,
            _params: null,
            messageVersions: null,
            tags: email.Tags.Count > 0 ? email.Tags.ToList() : null,
            scheduledAt: null,
            batchId: null);
    }

    private static List<SendSmtpEmailTo> MapTo(IReadOnlyList<EmailAddress> addresses)
    {
        return [.. addresses.Select(a => new SendSmtpEmailTo(a.Address, a.DisplayName))];
    }

    private static List<SendSmtpEmailCc>? MapCc(IReadOnlyList<EmailAddress> addresses)
    {
        return addresses.Count > 0
            ? [.. addresses.Select(a => new SendSmtpEmailCc(a.Address, a.DisplayName))]
            : null;
    }

    private static List<SendSmtpEmailBcc>? MapBcc(IReadOnlyList<EmailAddress> addresses)
    {
        return addresses.Count > 0
            ? [.. addresses.Select(a => new SendSmtpEmailBcc(a.Address, a.DisplayName))]
            : null;
    }
}
