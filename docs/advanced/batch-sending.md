# Batch Sending

Send multiple emails in bulk with concurrency control, delays, and configurable error handling via `IBatchEmailSender`.

## Interface

```csharp
namespace MailVolt.Core.Interfaces;

public interface IBatchEmailSender
{
    Task<BatchEmailResult> SendBatchAsync(
        IReadOnlyList<EmailMessage> emails,
        BatchSendOptions options,
        CancellationToken cancellationToken = default);
}
```

## BatchSendOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `MaxConcurrency` | `int` | `5` | Maximum parallel send operations |
| `DelayMs` | `int?` | `null` | Delay between each email (milliseconds) |
| `FailureStrategy` | `FailureStrategy` | `StopOnFirstFailure` | Behavior on send failure |

### FailureStrategy

| Value | Behavior |
|---|---|
| `StopOnFirstFailure` | Aborts the batch on the first failure |
| `Continue` | Continues sending remaining emails, collecting all results |

## BatchEmailResult

```csharp
public sealed record BatchEmailResult
{
    public int TotalCount { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public bool HasFailures => FailedCount > 0;
    public IReadOnlyList<(EmailMessage Message, EmailResult Result)> Results { get; init; }
}
```

## Usage

```csharp
public class NewsletterService
{
    private readonly IEmailBuilder _builder;
    private readonly IBatchEmailSender _batchSender;

    public NewsletterService(IEmailBuilder builder, IBatchEmailSender batchSender)
    {
        _builder = builder;
        _batchSender = batchSender;
    }

    public async Task<BatchEmailResult> SendNewsletterAsync(List<string> recipients)
    {
        var emails = new List<EmailMessage>();

        foreach (var email in recipients)
        {
            var message = await _builder
                .From("newsletter@example.com")
                .To(email)
                .Subject("Monthly Update")
                .HtmlBody("<h1>Your monthly newsletter</h1>")
                .BuildAsync();

            emails.Add(message);
        }

        return await _batchSender.SendBatchAsync(emails, new BatchSendOptions
        {
            MaxConcurrency = 10,
            DelayMs = 200,
            FailureStrategy = FailureStrategy.Continue
        });
    }
}
```

## Error Handling Per Email

With `FailureStrategy.Continue`, inspect individual results:

```csharp
var batchResult = await _batchSender.SendBatchAsync(emails, new BatchSendOptions
{
    FailureStrategy = FailureStrategy.Continue
});

if (batchResult.HasFailures)
{
    var failedEmails = batchResult.Results
        .Where(r => r.Result.IsFailure)
        .Select(r => new
        {
            Email = r.Message.To.First().Address,
            Error = r.Result.Error
        });

    foreach (var failed in failedEmails)
    {
        Console.WriteLine($"Failed: {failed.Email} - {failed.Error}");
    }
}

Console.WriteLine($"Sent {batchResult.SentCount}/{batchResult.TotalCount} emails");
```
