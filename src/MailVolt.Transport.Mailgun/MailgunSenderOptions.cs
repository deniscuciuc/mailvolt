namespace MailVolt.Transport.Mailgun;

/// <summary>
/// Options for configuring the Mailgun email transport.
/// </summary>
public sealed class MailgunSenderOptions
{
    /// <summary>
    /// The default configuration section name used when binding from <c>IConfiguration</c>.
    /// </summary>
    public const string SectionName = "MailVolt:Mailgun";

    /// <summary>
    /// The Mailgun API key (used as the password for Basic authentication).
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The Mailgun sending domain (e.g. "mg.example.com").
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// The base URL of the Mailgun API. Defaults to "https://api.mailgun.net/v3".
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.mailgun.net/v3";

    /// <summary>
    /// When <c>true</c>, the sender maps the reserved headers
    /// <c>X-MailVolt-Template</c> and <c>X-MailVolt-Template-Variables</c>
    /// to Mailgun's native <c>template</c> and <c>t:variables</c> fields.
    /// </summary>
    public bool UseNativeTemplates { get; set; }
}
