using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PostmarkDotNet;
using PostmarkDotNet.Model;

namespace MailVolt.Transport.Postmark;

/// <summary>
/// Sends email messages via the Postmark API using the official Postmark.NET SDK.
/// </summary>
internal sealed class PostmarkSender : IPostmarkSender
{
    private readonly IPostmarkClient _client;
    private readonly PostmarkSenderOptions _options;
    private readonly ILogger<PostmarkSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostmarkSender"/> class.
    /// </summary>
    /// <param name="client">The Postmark client used to send messages.</param>
    /// <param name="options">The Postmark sender options.</param>
    /// <param name="logger">The logger.</param>
    public PostmarkSender(
        IPostmarkClient client,
        IOptions<PostmarkSenderOptions> options,
        ILogger<PostmarkSender> logger)
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
            cancellationToken.ThrowIfCancellationRequested();

            var message = MapToPostmarkMessage(email, _options);
            var response = await _client.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.Status == PostmarkStatus.Success)
            {
                var messageId = response.MessageID == Guid.Empty
                    ? null
                    : response.MessageID.ToString();

                _logger.LogInformation(
                    "Email sent via Postmark. MessageID: {MessageID}, To: {To}",
                    messageId,
                    response.To);

                return EmailResult.Success(messageId);
            }

            _logger.LogError(
                "Postmark API returned {Status} (ErrorCode: {ErrorCode}): {Message}",
                response.Status,
                response.ErrorCode,
                response.Message);

            return EmailResult.Failure(
                $"Postmark API returned {response.Status} (ErrorCode: {response.ErrorCode}): {response.Message}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Postmark send operation was cancelled");
            return EmailResult.Failure("The send operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Postmark");
            return EmailResult.Failure($"Failed to send email via Postmark: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Maps a MailVolt <see cref="EmailMessage"/> to a <see cref="PostmarkMessage"/>.
    /// </summary>
    internal static PostmarkMessage MapToPostmarkMessage(EmailMessage email, PostmarkSenderOptions options)
    {
        var from = email.From?.ToString();
        if (string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "The email message must have a 'From' address specified.");
        }

        var message = new PostmarkMessage
        {
            From = from,
            To = FormatAddressList(email.To),
            Cc = FormatAddressListOrNull(email.Cc),
            Bcc = FormatAddressListOrNull(email.Bcc),
            ReplyTo = email.ReplyTo?.ToString(),
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
            TextBody = email.TextBody,
            Tag = email.Tags.Count > 0 ? email.Tags[0] : null,
            MessageStream = options.MessageStream,
        };

        if (email.Headers.Count > 0)
        {
            message.Headers = new HeaderCollection(new Dictionary<string, string>(email.Headers));
        }

        foreach (var attachment in email.Attachments)
        {
            using var memoryStream = new MemoryStream();
            attachment.Content.CopyTo(memoryStream);
            message.AddAttachment(
                memoryStream.ToArray(),
                attachment.FileName,
                attachment.ContentType,
                attachment.ContentId);
        }

        return message;
    }

    /// <summary>
    /// Formats a list of addresses as a comma-separated string suitable for Postmark.
    /// </summary>
    private static string FormatAddressList(IReadOnlyList<EmailAddress> addresses)
    {
        return addresses.Count == 0 ? string.Empty : string.Join(", ", addresses.Select(a => a.ToString()));
    }

    /// <summary>
    /// Formats a list of addresses as a comma-separated string, or returns null if the list is empty.
    /// </summary>
    private static string? FormatAddressListOrNull(IReadOnlyList<EmailAddress> addresses)
    {
        return addresses.Count == 0 ? null : FormatAddressList(addresses);
    }
}
