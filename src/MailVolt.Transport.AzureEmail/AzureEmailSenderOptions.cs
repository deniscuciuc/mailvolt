namespace MailVolt.Transport.AzureEmail;

/// <summary>
/// Configuration options for the Azure Email Communication Services sender.
/// </summary>
public sealed class AzureEmailSenderOptions
{
    /// <summary>
    /// Default configuration section name.
    /// </summary>
    public const string SectionName = "MailVolt:Azure";

    /// <summary>
    /// The Azure Communication Services connection string.
    /// </summary>
    public required string ConnectionString { get; set; }
}
