using PostmarkDotNet;

namespace MailVolt.Transport.Postmark;

/// <summary>
/// Abstraction over the official Postmark client to enable unit testing.
/// </summary>
internal interface IPostmarkClient
{
    /// <summary>
    /// Sends a single email through the Postmark API.
    /// </summary>
    Task<PostmarkResponse> SendMessageAsync(
        PostmarkMessage message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Wraps the official <see cref="PostmarkClient"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PostmarkClientWrapper"/> class.
/// </remarks>
/// <param name="serverToken">The Postmark server API token.</param>
/// <param name="apiBaseUri">The Postmark API base URI.</param>
internal sealed class PostmarkClientWrapper(string serverToken, string apiBaseUri) : IPostmarkClient
{
    private readonly PostmarkClient _client = new(serverToken, apiBaseUri);

    /// <inheritdoc />
    public Task<PostmarkResponse> SendMessageAsync(
        PostmarkMessage message,
        CancellationToken cancellationToken = default)
    {
        return _client.SendMessageAsync(message);
    }
}
