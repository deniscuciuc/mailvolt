namespace MailVolt.Transport.Resend;

/// <summary>
/// Configuration options for the Resend email transport.
/// </summary>
public sealed class ResendSenderOptions
{
    /// <summary>
    /// The default configuration section name used to bind these options.
    /// </summary>
    public const string SectionName = "MailVolt:Resend";

    /// <summary>
    /// The Resend API key.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The base URL of the Resend API. Defaults to <c>https://api.resend.com</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.resend.com";
}
