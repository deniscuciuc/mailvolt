namespace MailVolt.Core.Models;

/// <summary>
/// Represents the aggregated result of sending a batch of emails.
/// </summary>
public sealed record BatchEmailResult
{
    /// <summary>
    /// The total number of emails processed in the batch.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// The number of emails that were sent successfully.
    /// </summary>
    public int SentCount { get; init; }

    /// <summary>
    /// The number of emails that failed to send.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// The number of emails that were never attempted, because
    /// <see cref="Interfaces.FailureStrategy.StopOnFirstFailure"/> halted the batch or the
    /// caller cancelled it. <see cref="SentCount"/>, <see cref="FailedCount"/> and
    /// <see cref="SkippedCount"/> always sum to <see cref="TotalCount"/>.
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Whether any failures occurred during batch sending.
    /// </summary>
    public bool HasFailures => FailedCount > 0;

    /// <summary>
    /// Individual results for each attempted email, paired with the original message.
    /// </summary>
    /// <remarks>
    /// Ordered by completion, not by the order of the input list, and shorter than
    /// <see cref="TotalCount"/> when <see cref="SkippedCount"/> is non-zero.
    /// </remarks>
    public IReadOnlyList<(EmailMessage Message, EmailResult Result)> Results { get; init; } = [];
}
