using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;

namespace MailVolt.Transport.Resend;

/// <summary>
/// Sends emails via the Resend REST API (<c>POST /emails</c>).
/// </summary>
internal sealed class ResendSender : IResendSender
{
    private const string EmailsEndpoint = "emails";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ResendSender> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResendSender"/> class.
    /// </summary>
    /// <param name="httpClient">The typed <see cref="HttpClient"/> configured with the base URL and auth header.</param>
    /// <param name="logger">The logger.</param>
    public ResendSender(
        HttpClient httpClient,
        ILogger<ResendSender> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var payload = MapToPayload(email);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EmailsEndpoint)
            {
                Content = JsonContent.Create(payload, options: JsonOptions),
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var errorMessage = $"Resend API returned {(int)response.StatusCode}: {errorBody}";

                _logger.LogError("Failed to send email via Resend. Status: {StatusCode}, Response: {ResponseBody}",
                    (int)response.StatusCode, errorBody);

                return EmailResult.Failure(errorMessage);
            }

            var responseBody = await response.Content.ReadFromJsonAsync<ResendSendResponse>(JsonOptions, cancellationToken);

            if (responseBody?.Id is { Length: > 0 } messageId)
            {
                _logger.LogInformation("Email sent successfully via Resend. MessageId: {MessageId}", messageId);
                return EmailResult.Success(messageId);
            }

            _logger.LogWarning("Resend API returned success but no message id was present.");
            return EmailResult.Success();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Email sending via Resend was cancelled.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to Resend API failed.");
            return EmailResult.Failure("HTTP request to Resend API failed.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Resend API response.");
            return EmailResult.Failure("Failed to deserialize Resend API response.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "An unexpected error occurred while sending email via Resend.");
            return EmailResult.Failure("An unexpected error occurred.", ex);
        }
    }

    private static ResendEmailPayload MapToPayload(EmailMessage email)
    {
        var payload = new ResendEmailPayload
        {
            From = email.From?.ToString() ?? string.Empty,
            To = email.To.Select(addr => addr.ToString()).ToList(),
            Subject = email.Subject,
        };

        if (email.Cc.Count > 0)
        {
            payload.Cc = email.Cc.Select(addr => addr.ToString()).ToList();
        }

        if (email.Bcc.Count > 0)
        {
            payload.Bcc = email.Bcc.Select(addr => addr.ToString()).ToList();
        }

        if (email.ReplyTo is not null)
        {
            payload.ReplyTo = [email.ReplyTo.ToString()];
        }

        if (email.HtmlBody is { Length: > 0 })
        {
            payload.Html = email.HtmlBody;
        }

        if (email.TextBody is { Length: > 0 })
        {
            payload.Text = email.TextBody;
        }

        if (email.Attachments.Count > 0)
        {
            payload.Attachments = email.Attachments
                .Select(MapAttachment)
                .ToList();
        }

        if (email.Headers.Count > 0)
        {
            payload.Headers = new Dictionary<string, string>(email.Headers);
        }

        if (email.Tags.Count > 0)
        {
            payload.Tags = email.Tags
                .Select(tag => new ResendTag { Name = tag, Value = tag })
                .ToList();
        }

        return payload;
    }

    private static ResendAttachment MapAttachment(EmailAttachment attachment)
    {
        var contentBytes = ReadStreamFully(attachment.Content);
        var base64Content = Convert.ToBase64String(contentBytes);

        return new ResendAttachment
        {
            Filename = attachment.FileName,
            Content = base64Content,
            ContentType = attachment.ContentType,
        };
    }

    private static byte[] ReadStreamFully(Stream stream)
    {
        if (stream.CanSeek)
        {
            using var ms = new MemoryStream((int)stream.Length);
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    // ──────────────────────────────────────────────────────────
    //  Internal DTOs – mapped to the Resend API JSON contract
    // ──────────────────────────────────────────────────────────

    private sealed record ResendSendResponse
    {
        public string? Id { get; init; }
    }

    private sealed class ResendEmailPayload
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public List<string> To { get; set; } = [];

        [JsonPropertyName("cc")]
        public List<string>? Cc { get; set; }

        [JsonPropertyName("bcc")]
        public List<string>? Bcc { get; set; }

        [JsonPropertyName("reply_to")]
        public List<string>? ReplyTo { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("html")]
        public string? Html { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("tags")]
        public List<ResendTag>? Tags { get; set; }

        [JsonPropertyName("attachments")]
        public List<ResendAttachment>? Attachments { get; set; }
    }

    private sealed class ResendAttachment
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = string.Empty;
    }

    private sealed class ResendTag
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}
