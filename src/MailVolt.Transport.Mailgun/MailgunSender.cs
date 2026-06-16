using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailVolt.Core.Models;
using Microsoft.Extensions.Options;

namespace MailVolt.Transport.Mailgun;

/// <summary>
/// Sends email messages via the Mailgun REST API using a typed <see cref="HttpClient"/>.
/// </summary>
internal sealed class MailgunSender : IMailgunSender
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Dictionary<EmailPriority, string> PriorityMap = new()
    {
        [EmailPriority.Low] = "5",
        [EmailPriority.Normal] = "3",
        [EmailPriority.High] = "1",
    };

    private readonly HttpClient _httpClient;
    private readonly MailgunSenderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MailgunSender"/> class.
    /// </summary>
    /// <param name="httpClient">The typed <see cref="HttpClient"/> provided by the HTTP client factory.</param>
    /// <param name="options">The Mailgun sender options.</param>
    public MailgunSender(HttpClient httpClient, IOptions<MailgunSenderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            ("api:" + _options.ApiKey).ToBase64Text());
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = BuildMultipartContent(email);
            var response = await _httpClient.PostAsync(
                $"{_options.Domain}/messages",
                content,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return EmailResult.Failure(
                    $"Mailgun responded with {(int)response.StatusCode}: {body}");
            }

            var result = JsonSerializer.Deserialize<MailgunSendResponse>(body, JsonOptions);

            if (result?.Id is null)
            {
                return EmailResult.Failure(
                    "Mailgun response did not contain a message ID.");
            }

            return EmailResult.Success(result.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EmailResult.Failure(
                "An exception occurred while sending the email via Mailgun.",
                ex);
        }
    }

    private MultipartFormDataContent BuildMultipartContent(EmailMessage email)
    {
        var content = new MultipartFormDataContent();

        // --- Recipients (Mailgun accepts comma-separated values) ---
        if (email.From is not null)
        {
            content.Add(new StringContent(email.From.ToString()), "from");
        }

        AddAddresses(content, "to", email.To);
        AddAddresses(content, "cc", email.Cc);
        AddAddresses(content, "bcc", email.Bcc);

        // --- Subject ---
        content.Add(new StringContent(email.Subject), "subject");

        // --- Body ---
        if (email.TextBody is { Length: > 0 })
        {
            content.Add(new StringContent(email.TextBody), "text");
        }

        if (email.HtmlBody is { Length: > 0 })
        {
            content.Add(new StringContent(email.HtmlBody), "html");
        }

        // --- Reply-To ---
        if (email.ReplyTo is not null)
        {
            content.Add(new StringContent(email.ReplyTo.ToString()), "h:Reply-To");
        }

        // --- Priority ---
        if (email.Priority != EmailPriority.Normal && PriorityMap.TryGetValue(email.Priority, out var priorityValue))
        {
            content.Add(new StringContent(priorityValue), "h:X-Priority");
        }

        // --- Custom headers ---
        foreach (var (key, value) in email.Headers)
        {
            content.Add(new StringContent(value), $"h:{key}");
        }

        // --- Tags ---
        foreach (var tag in email.Tags)
        {
            content.Add(new StringContent(tag), "o:tag");
        }

        // --- Attachments and inline images ---
        foreach (var attachment in email.Attachments)
        {
            var streamContent = new StreamContent(attachment.Content);

            if (attachment.ContentType is { Length: > 0 })
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType);
            }

            if (attachment.IsInline)
            {
                // Inline image — set Content-ID and use "inline" disposition
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                {
                    FileName = attachment.FileName,
                };

                if (!string.IsNullOrEmpty(attachment.ContentId))
                {
                    streamContent.Headers.Add("Content-ID", $"<{attachment.ContentId}>");
                }

                content.Add(streamContent, "inline", attachment.FileName);
            }
            else
            {
                // Regular attachment — use "attachment" disposition
                content.Add(streamContent, "attachment", attachment.FileName);
            }
        }

        return content;
    }

    private static void AddAddresses(MultipartFormDataContent content, string fieldName, IReadOnlyList<EmailAddress> addresses)
    {
        if (addresses.Count == 0)
        {
            return;
        }

        // Mailgun accepts multiple recipients as multiple field entries with the same name
        foreach (var address in addresses)
        {
            content.Add(new StringContent(address.ToString()), fieldName);
        }
    }

    /// <summary>
    /// Lightweight model for deserializing the Mailgun send response.
    /// </summary>
    private sealed record MailgunSendResponse
    {
        public string? Id { get; init; }
        public string? Message { get; init; }
    }
}

/// <summary>
/// Extension methods to simplify base-64 encoding.
/// </summary>
file static class Base64Extensions
{
    /// <summary>
    /// Converts a UTF-8 string to its Base-64 encoded representation.
    /// </summary>
    public static string ToBase64Text(this string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}
