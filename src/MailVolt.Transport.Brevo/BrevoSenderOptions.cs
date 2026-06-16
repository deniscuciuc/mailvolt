namespace MailVolt.Transport.Brevo;

/// <summary>
/// Configuration options for the Brevo (formerly Sendinblue) email sender.
/// </summary>
public sealed class BrevoSenderOptions
{
    /// <summary>
    /// Default configuration section name.
    /// </summary>
    public const string SectionName = "MailVolt:Brevo";

    /// <summary>
    /// The Brevo API key used for authentication.
    /// </summary>
    public required string ApiKey { get; set; }
}
