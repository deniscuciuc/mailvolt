using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Options;

namespace MailVolt.Transport.Brevo;

/// <summary>
/// Sends email via the Brevo (formerly Sendinblue) v3 API.
/// </summary>
public sealed class BrevoSender : IBrevoSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoSenderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrevoSender"/> class.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> configured with the Brevo base address and resilience.</param>
    /// <param name="options">The Brevo sender options containing the API key.</param>
    public BrevoSender(HttpClient httpClient, IOptions<BrevoSenderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            var request = BuildRequest(email);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v3/smtp/email")
            {
                Headers = { { "api-key", _options.ApiKey } },
                Content = JsonContent.Create(request),
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BrevoSendResponse>(cancellationToken: cancellationToken);

            return EmailResult.Success(result?.MessageId);
        }
        catch (Exception ex)
        {
            return EmailResult.Failure(ex.Message, ex);
        }
    }

    private static BrevoSendRequest BuildRequest(EmailMessage email)
    {
        var sender = email.From is not null
            ? new BrevoAddress { Name = email.From.DisplayName, Email = email.From.Address }
            : null;

        BrevoAddress? replyTo = email.ReplyTo is not null
            ? new BrevoAddress { Name = email.ReplyTo.DisplayName, Email = email.ReplyTo.Address }
            : null;

        var attachments = new List<BrevoAttachment>(email.Attachments.Count);

        foreach (var attachment in email.Attachments)
        {
            using var ms = new MemoryStream();
            attachment.Content.CopyTo(ms);
            attachments.Add(new BrevoAttachment
            {
                Name = attachment.FileName,
                Content = Convert.ToBase64String(ms.ToArray()),
            });
        }

        return new BrevoSendRequest
        {
            Sender = sender,
            To = MapAddresses(email.To),
            Cc = MapOptionalAddresses(email.Cc),
            Bcc = MapOptionalAddresses(email.Bcc),
            ReplyTo = replyTo,
            Subject = email.Subject,
            HtmlContent = email.HtmlBody,
            TextContent = email.TextBody,
            Attachments = attachments.Count > 0 ? attachments : null,
        };
    }

    private static List<BrevoAddress> MapAddresses(IReadOnlyList<EmailAddress> addresses)
    {
        return addresses.Select(a => new BrevoAddress { Name = a.DisplayName, Email = a.Address }).ToList();
    }

    private static List<BrevoAddress>? MapOptionalAddresses(IReadOnlyList<EmailAddress> addresses)
    {
        return addresses.Count > 0 ? MapAddresses(addresses) : null;
    }

    // ──────────────────────────────
    //  Internal request / response DTOs
    // ──────────────────────────────

    private sealed class BrevoSendRequest
    {
        [JsonPropertyName("sender")]
        public BrevoAddress? Sender { get; set; }

        [JsonPropertyName("to")]
        public List<BrevoAddress> To { get; set; } = [];

        [JsonPropertyName("cc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BrevoAddress>? Cc { get; set; }

        [JsonPropertyName("bcc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BrevoAddress>? Bcc { get; set; }

        [JsonPropertyName("replyTo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public BrevoAddress? ReplyTo { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("htmlContent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HtmlContent { get; set; }

        [JsonPropertyName("textContent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextContent { get; set; }

        [JsonPropertyName("attachment")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<BrevoAttachment>? Attachments { get; set; }
    }

    private sealed class BrevoAddress
    {
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class BrevoAttachment
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class BrevoSendResponse
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }
    }
}
