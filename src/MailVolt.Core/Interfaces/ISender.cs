using MailVolt.Core.Models;

namespace MailVolt.Core.Interfaces;

/// <summary>
/// Abstraction for sending a single email via a specific provider or transport.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends the specified email message.
    /// </summary>
    /// <param name="email">The email message to send.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="EmailResult"/> indicating success or failure.</returns>
    Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default);
}
