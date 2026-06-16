using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class DigestWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DigestWorker> logger) : BackgroundService
{
    // Send at 08:00 every day, check every minute
    private static readonly TimeOnly SendTime = new(8, 0);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("DigestWorker started. Will send at {Time} daily.", SendTime);

        while (!ct.IsCancellationRequested)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            if (now.Hour == SendTime.Hour && now.Minute == SendTime.Minute)
            {
                await SendDailyDigestAsync(ct);
                // Sleep 61s to avoid double-send within same minute
                await Task.Delay(TimeSpan.FromSeconds(61), ct);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
        }
    }

    private async Task SendDailyDigestAsync(CancellationToken ct)
    {
        // IEmailBuilder is Transient — must be resolved from a scope
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailBuilder = scope.ServiceProvider.GetRequiredService<IEmailBuilder>();
        var batchSender  = scope.ServiceProvider.GetRequiredService<IBatchEmailSender>();

        var subscribers = GetSubscribers();
        var model = new DigestModel(
            Date:     DateOnly.FromDateTime(DateTime.Today),
            Articles: ["New in .NET 10", "MailVolt 1.0 released", "C# 14 tips & tricks"]
        );

        var messages = new List<EmailMessage>();
        foreach (var sub in subscribers)
        {
            var msg = await emailBuilder
                .To(new EmailAddress(sub.Email, sub.Name))
                .Subject($"📰 Daily Digest — {model.Date:MMMM dd, yyyy}")
                .UsingTemplate("Templates/Digest.liquid", model with { SubscriberName = sub.Name })
                .BuildAsync(ct);
            messages.Add(msg);
        }

        var result = await batchSender.SendBatchAsync(messages,
            new BatchSendOptions { MaxConcurrency = 3, DelayMs = 200 }, ct);

        logger.LogInformation("Digest sent: {Sent}/{Total} succeeded.", result.SentCount, result.TotalCount);
    }

    private static List<(string Email, string Name)> GetSubscribers() =>
    [
        ("alice@example.com", "Alice"),
        ("bob@example.com",   "Bob"),
        ("carol@example.com", "Carol"),
    ];
}

public record DigestModel(DateOnly Date, string[] Articles, string SubscriberName = "");
