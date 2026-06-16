namespace MailVolt.Transport.Postmark;

/// <summary>
/// Configuration options for the Postmark email transport.
/// </summary>
public sealed class PostmarkSenderOptions
{
    /// <summary>
    /// The default configuration section name.
    /// </summary>
    public const string SectionName = "MailVolt:Postmark";

    /// <summary>
    /// The Postmark server API token. Required.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The message stream to use when sending. Defaults to <c>"outbound"</c>.
    /// </summary>
    public string MessageStream { get; set; } = "outbound";

    /// <summary>
    /// The Postmark API base URL. Defaults to <c>"https://api.postmarkapp.com"</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.postmarkapp.com";
}
