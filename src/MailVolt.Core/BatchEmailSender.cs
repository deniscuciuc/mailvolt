using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;

namespace MailVolt.Core;

/// <summary>
/// Sends a batch of emails with configurable concurrency and failure handling.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BatchEmailSender"/> class.
/// </remarks>
/// <param name="sender">The underlying email sender used for each individual message.</param>
internal sealed class BatchEmailSender(ISender sender) : IBatchEmailSender
{
    /// <inheritdoc />
    public async Task<BatchEmailResult> SendBatchAsync(
        IReadOnlyList<EmailMessage> emails,
        BatchSendOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emails);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxConcurrency, 1);

        if (options.DelayMs is { } configuredDelay)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(configuredDelay, nameof(options.DelayMs));
        }

        if (emails.Count == 0)
        {
            return new BatchEmailResult
            {
                TotalCount = 0,
                SentCount = 0,
                FailedCount = 0,
                SkippedCount = 0,
                Results = [],
            };
        }

        var resultsLock = new object();
        var results = new List<(EmailMessage Message, EmailResult Result)>(emails.Count);

        using var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

        // A linked token lets StopOnFirstFailure cancel the sends that have not started yet.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var combinedToken = linkedCts.Token;

        var tasks = emails
            .Select(email =>
                SendOneAsync(email, semaphore, results, resultsLock, options, linkedCts, combinedToken))
            .ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // A cancellation the caller asked for is an error; one this method triggered to stop
        // the batch early is not.
        cancellationToken.ThrowIfCancellationRequested();

        var sentCount = results.Count(r => r.Result.IsSuccess);
        var failedCount = results.Count - sentCount;

        return new BatchEmailResult
        {
            TotalCount = emails.Count,
            SentCount = sentCount,
            FailedCount = failedCount,
            // Emails the batch never attempted are reported explicitly, so the three counts
            // always add up to TotalCount.
            SkippedCount = emails.Count - results.Count,
            Results = results.AsReadOnly(),
        };
    }

    private async Task SendOneAsync(
        EmailMessage email,
        SemaphoreSlim semaphore,
        List<(EmailMessage Message, EmailResult Result)> results,
        object resultsLock,
        BatchSendOptions options,
        CancellationTokenSource linkedCts,
        CancellationToken cancellationToken)
    {
        var acquired = false;

        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;

            cancellationToken.ThrowIfCancellationRequested();

            var result = await sender.SendAsync(email, cancellationToken).ConfigureAwait(false);

            lock (resultsLock)
            {
                results.Add((email, result));
            }

            if (result.IsFailure && options.FailureStrategy == FailureStrategy.StopOnFirstFailure)
            {
                await linkedCts.CancelAsync().ConfigureAwait(false);
                return;
            }

            // Deliberately inside the semaphore: holding the slot for the delay is what
            // caps the send rate at roughly MaxConcurrency / (sendTime + DelayMs). Moving
            // it outside would let every queued email start immediately and defeat the
            // rate limiting this option exists for.
            if (options.DelayMs is { } delay)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when StopOnFirstFailure cancels the remaining sends, or when the
            // caller cancels. Either way this email is reported as skipped.
        }
        finally
        {
            if (acquired)
            {
                semaphore.Release();
            }
        }
    }
}
