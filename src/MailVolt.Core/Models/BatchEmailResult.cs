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
    /// Whether any failures occurred during batch sending.
    /// </summary>
    public bool HasFailures => FailedCount > 0;

    /// <summary>
    /// Individual results for each email in the batch, paired with the original message.
    /// </summary>
    public IReadOnlyList<(EmailMessage Message, EmailResult Result)> Results { get; init; } = [];
}
