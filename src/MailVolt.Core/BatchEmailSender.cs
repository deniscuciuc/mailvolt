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

        if (emails.Count == 0)
        {
            return new BatchEmailResult
            {
                TotalCount = 0,
                SentCount = 0,
                FailedCount = 0,
                Results = [],
            };
        }

        var resultsLock = new object();
        var results = new List<(EmailMessage Message, EmailResult Result)>(emails.Count);
        var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);

        // Use a linked token so remaining tasks can be cancelled on StopOnFirstFailure.
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var combinedToken = linkedCts.Token;

        var tasks = new List<Task>(emails.Count);
        tasks.AddRange(emails.Select(email =>
            SendOneAsync(email, semaphore, results, resultsLock, options, linkedCts, combinedToken)));

        await Task.WhenAll(tasks);

        // If the user's token was cancelled, throw with the original token.
        cancellationToken.ThrowIfCancellationRequested();

        var sentCount = results.Count(r => r.Result.IsSuccess);
        var failedCount = results.Count - sentCount;

        return new BatchEmailResult
        {
            TotalCount = emails.Count,
            SentCount = sentCount,
            FailedCount = failedCount,
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
            await semaphore.WaitAsync(cancellationToken);
            acquired = true;

            cancellationToken.ThrowIfCancellationRequested();

            var result = await sender.SendAsync(email, cancellationToken);

            lock (resultsLock)
            {
                results.Add((email, result));
            }

            if (result.IsFailure && options.FailureStrategy == FailureStrategy.StopOnFirstFailure)
            {
                await linkedCts.CancelAsync();
                return;
            }

            if (options.DelayMs is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Suppress cancellation exceptions — they are expected when StopOnFirstFailure cancels remaining tasks.
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
