namespace MailVolt.Transport.Postmark;

/// <summary>
/// Constants used when configuring the named <see cref="System.Net.Http.HttpClient"/>
/// for Postmark API calls.
/// </summary>
internal static class PostmarkHttpClient
{
    /// <summary>
    /// The name of the typed / named HttpClient used by <see cref="PostmarkSender"/>.
    /// </summary>
    public const string Name = "MailVolt.Postmark";
}
