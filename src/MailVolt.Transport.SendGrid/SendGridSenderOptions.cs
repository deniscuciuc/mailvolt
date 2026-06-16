namespace MailVolt.Transport.SendGrid;

/// <summary>
/// Options for configuring the SendGrid email sender.
/// </summary>
public sealed class SendGridSenderOptions
{
    /// <summary>
    /// Default configuration section name for binding from <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "MailVolt:SendGrid";

    /// <summary>
    /// The SendGrid API key used for authentication.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The base URL of the SendGrid API. Defaults to <c>https://api.sendgrid.com</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.sendgrid.com";

    /// <summary>
    /// When <c>true</c>, emails without a text or HTML body are assumed to use SendGrid dynamic templates.
    /// </summary>
    public bool UseDynamicTemplates { get; set; }

    /// <summary>
    /// Optional sandbox mode setting. When set to <c>"true"</c> (case-insensitive),
    /// the SendGrid mail/sandbox mode setting is enabled.
    /// </summary>
    public string? SandboxMode { get; set; }
}
