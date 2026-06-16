using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailVolt.Transport.Postmark;

/// <summary>
/// Sends email messages via the Postmark API.
/// </summary>
internal sealed class PostmarkSender : IPostmarkSender
{
    private readonly HttpClient _httpClient;
    private readonly PostmarkSenderOptions _options;
    private readonly ILogger<PostmarkSender> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PostmarkSender(
        IHttpClientFactory httpClientFactory,
        IOptions<PostmarkSenderOptions> options,
        ILogger<PostmarkSender> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(PostmarkHttpClient.Name);
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            var request = MapToRequest(email);
            var response = await _httpClient.PostAsJsonAsync(
                "/email",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Postmark API returned {StatusCode}: {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody);
                return EmailResult.Failure(
                    $"Postmark API returned {(int)response.StatusCode}: {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<PostmarkSendResponse>(
                JsonOptions,
                cancellationToken);

            if (result is null)
            {
                _logger.LogError("Postmark API returned an empty response body");
                return EmailResult.Failure("Postmark API returned an empty response body");
            }

            _logger.LogInformation(
                "Email sent via Postmark. MessageID: {MessageID}, To: {To}",
                result.MessageID,
                result.To);

            return EmailResult.Success(result.MessageID);
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
    /// Maps a MailVolt <see cref="EmailMessage"/> to a <see cref="PostmarkSendRequest"/>.
    /// </summary>
    private PostmarkSendRequest MapToRequest(EmailMessage email)
    {
        var from = email.From?.ToString();
        if (string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "The email message must have a 'From' address specified.");
        }

        return new PostmarkSendRequest
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
            Headers = email.Headers.Count > 0
                ? email.Headers
                    .Select(kvp => new PostmarkHeader { Name = kvp.Key, Value = kvp.Value })
                    .ToList()
                : null,
            Attachments = email.Attachments.Count > 0
                ? email.Attachments.Select(MapAttachment).ToList()
                : null,
            MessageStream = _options.MessageStream,
        };
    }

    /// <summary>
    /// Maps an <see cref="EmailAttachment"/> to a <see cref="PostmarkAttachment"/>,
    /// converting the content stream to a base64 string.
    /// </summary>
    private static PostmarkAttachment MapAttachment(EmailAttachment attachment)
    {
        using var memoryStream = new MemoryStream();
        attachment.Content.CopyTo(memoryStream);
        var base64Content = Convert.ToBase64String(memoryStream.ToArray());

        return new PostmarkAttachment
        {
            Name = attachment.FileName,
            Content = base64Content,
            ContentType = attachment.ContentType,
            ContentID = attachment.ContentId,
        };
    }

    /// <summary>
    /// Formats a list of addresses as a comma-separated string suitable for Postmark.
    /// </summary>
    private static string FormatAddressList(IReadOnlyList<EmailAddress> addresses)
    {
        if (addresses.Count == 0)
            return string.Empty;

        return string.Join(", ", addresses.Select(a => a.ToString()));
    }

    /// <summary>
    /// Formats a list of addresses as a comma-separated string, or returns null if the list is empty.
    /// </summary>
    private static string? FormatAddressListOrNull(IReadOnlyList<EmailAddress> addresses)
    {
        if (addresses.Count == 0)
            return null;

        return FormatAddressList(addresses);
    }

    // ──────────────────────────────────────────────
    //  Internal DTOs for Postmark API communication
    // ──────────────────────────────────────────────

    internal sealed record PostmarkSendRequest
    {
        [JsonPropertyName("From")]
        public required string From { get; init; }

        [JsonPropertyName("To")]
        public required string To { get; init; }

        [JsonPropertyName("Cc")]
        public string? Cc { get; init; }

        [JsonPropertyName("Bcc")]
        public string? Bcc { get; init; }

        [JsonPropertyName("ReplyTo")]
        public string? ReplyTo { get; init; }

        [JsonPropertyName("Subject")]
        public required string Subject { get; init; }

        [JsonPropertyName("HtmlBody")]
        public string? HtmlBody { get; init; }

        [JsonPropertyName("TextBody")]
        public string? TextBody { get; init; }

        [JsonPropertyName("Tag")]
        public string? Tag { get; init; }

        [JsonPropertyName("Headers")]
        public IReadOnlyList<PostmarkHeader>? Headers { get; init; }

        [JsonPropertyName("Attachments")]
        public IReadOnlyList<PostmarkAttachment>? Attachments { get; init; }

        [JsonPropertyName("MessageStream")]
        public string? MessageStream { get; init; }
    }

    internal sealed record PostmarkHeader
    {
        [JsonPropertyName("Name")]
        public required string Name { get; init; }

        [JsonPropertyName("Value")]
        public required string Value { get; init; }
    }

    internal sealed record PostmarkAttachment
    {
        [JsonPropertyName("Name")]
        public required string Name { get; init; }

        [JsonPropertyName("Content")]
        public required string Content { get; init; }

        [JsonPropertyName("ContentType")]
        public required string ContentType { get; init; }

        [JsonPropertyName("ContentID")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContentID { get; init; }
    }

    internal sealed record PostmarkSendResponse
    {
        [JsonPropertyName("To")]
        public string? To { get; init; }

        [JsonPropertyName("SubmittedAt")]
        public string? SubmittedAt { get; init; }

        [JsonPropertyName("MessageID")]
        public string? MessageID { get; init; }

        [JsonPropertyName("ErrorCode")]
        public int ErrorCode { get; init; }

        [JsonPropertyName("Message")]
        public string? Message { get; init; }
    }
}
