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
    private const string NativeTemplateHeaderName = "X-MailVolt-Template";
    private const string NativeTemplateVariablesHeaderName = "X-MailVolt-Template-Variables";
    private const string MailgunTemplateFieldName = "template";
    private const string MailgunTemplateVariablesFieldName = "t:variables";

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

            return result?.Id is null
                ? EmailResult.Failure(
                    "Mailgun response did not contain a message ID.")
                : EmailResult.Success(result.Id);
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
        var nativeTemplate = TryGetHeaderValue(email.Headers, NativeTemplateHeaderName);
        var nativeTemplateVariables = TryGetHeaderValue(email.Headers, NativeTemplateVariablesHeaderName);
        ValidateNativeTemplateHeaders(nativeTemplate, nativeTemplateVariables);
        var usingNativeTemplate = _options.UseNativeTemplates && nativeTemplate is not null;

        if (email.From is not null)
        {
            content.Add(new StringContent(email.From.ToString()), "from");
        }

        AddAddresses(content, "to", email.To);
        AddAddresses(content, "cc", email.Cc);
        AddAddresses(content, "bcc", email.Bcc);

        content.Add(new StringContent(email.Subject), "subject");

        switch (usingNativeTemplate)
        {
            case true:
            {
                content.Add(new StringContent(nativeTemplate!), MailgunTemplateFieldName);

                if (nativeTemplateVariables is { } templateVariables)
                {
                    content.Add(new StringContent(templateVariables), MailgunTemplateVariablesFieldName);
                }

                break;
            }
            case false when email.TextBody is { Length: > 0 }:
                content.Add(new StringContent(email.TextBody), "text");
                break;
        }

        if (!usingNativeTemplate && email.HtmlBody is { Length: > 0 })
        {
            content.Add(new StringContent(email.HtmlBody), "html");
        }

        if (email.ReplyTo is not null)
        {
            content.Add(new StringContent(email.ReplyTo.ToString()), "h:Reply-To");
        }

        if (email.Priority != EmailPriority.Normal && PriorityMap.TryGetValue(email.Priority, out var priorityValue))
        {
            content.Add(new StringContent(priorityValue), "h:X-Priority");
        }

        foreach (var (key, value) in email.Headers)
        {
            if (IsNativeTemplateHeader(key))
            {
                continue;
            }

            content.Add(new StringContent(value), $"h:{key}");
        }

        foreach (var tag in email.Tags)
        {
            content.Add(new StringContent(tag), "o:tag");
        }

        foreach (var attachment in email.Attachments)
        {
            var streamContent = new StreamContent(attachment.Content);

            if (attachment.ContentType is { Length: > 0 })
            {
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType);
            }

            if (attachment.IsInline)
            {
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
                content.Add(streamContent, "attachment", attachment.FileName);
            }
        }

        return content;
    }

    private void ValidateNativeTemplateHeaders(string? nativeTemplate, string? nativeTemplateVariables)
    {
        if (!_options.UseNativeTemplates || nativeTemplateVariables is null || nativeTemplate is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"'{NativeTemplateVariablesHeaderName}' requires '{NativeTemplateHeaderName}' when native templates are enabled.");
    }

    private static void AddAddresses(MultipartFormDataContent content, string fieldName,
        IReadOnlyList<EmailAddress> addresses)
    {
        if (addresses.Count == 0)
        {
            return;
        }

        foreach (var address in addresses)
        {
            content.Add(new StringContent(address.ToString()), fieldName);
        }
    }

    private static bool IsNativeTemplateHeader(string key) =>
        key.Equals(NativeTemplateHeaderName, StringComparison.OrdinalIgnoreCase) ||
        key.Equals(NativeTemplateVariablesHeaderName, StringComparison.OrdinalIgnoreCase);

    private static string? TryGetHeaderValue(
        IReadOnlyDictionary<string, string> headers,
        string headerName)
    {
        foreach (var (key, value) in headers)
        {
            if (key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
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
