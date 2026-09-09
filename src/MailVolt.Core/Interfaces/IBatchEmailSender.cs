using MailVolt.Core.Models;

namespace MailVolt.Core.Interfaces;

/// <summary>
/// Defines the strategy to apply when an email in a batch fails.
/// </summary>
public enum FailureStrategy
{
    /// <summary>
    /// Stop sending remaining emails when the first failure occurs.
    /// </summary>
    StopOnFirstFailure,

    /// <summary>
    /// Continue sending remaining emails even after failures.
    /// </summary>
    Continue,
}

/// <summary>
/// Options for controlling batch email sending behaviour.
/// </summary>
/// <param name="MaxConcurrency">Maximum number of concurrent send operations. Must be greater than zero. Defaults to 5.</param>
/// <param name="DelayMs">
/// Optional delay (in milliseconds) applied after each send while still holding the
/// concurrency slot, capping the send rate at roughly
/// <paramref name="MaxConcurrency"/> / (send time + <paramref name="DelayMs"/>).
/// Must not be negative.
/// </param>
/// <param name="FailureStrategy">Strategy to apply when a send fails. Defaults to <see cref="FailureStrategy.StopOnFirstFailure"/>.</param>
public sealed record BatchSendOptions(
    int MaxConcurrency = 5,
    int? DelayMs = null,
    FailureStrategy FailureStrategy = FailureStrategy.StopOnFirstFailure);

/// <summary>
/// Abstraction for sending multiple emails in a batch with concurrency control and error handling.
/// </summary>
public interface IBatchEmailSender
{
    /// <summary>
    /// Sends a batch of emails with the specified options.
    /// </summary>
    /// <param name="emails">The email messages to send.</param>
    /// <param name="options">Options controlling concurrency, delay, and failure behaviour.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="BatchEmailResult"/> aggregating the results.</returns>
    Task<BatchEmailResult> SendBatchAsync(
        IReadOnlyList<EmailMessage> emails,
        BatchSendOptions options,
        CancellationToken cancellationToken = default);
}
