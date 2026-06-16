using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Transport.Smtp.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMailVolt(builder.Configuration)
    .UseSmtpTransport(options =>
    {
        options.Host = "localhost";
        options.Port = 1025;
    });

builder.Services.AddHostedService<BatchEmailWorker>();

var host = builder.Build();
host.Run();

public sealed class BatchEmailWorker(
    IEmailBuilder emailBuilder,
    IBatchEmailSender batchSender,
    ILogger<BatchEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Batch email worker starting");

        var emails = new List<EmailMessage>();
        for (var i = 0; i < 10; i++)
        {
            var msg = await emailBuilder
                .From("batch@example.com")
                .To($"user{i}@example.com")
                .Subject($"Batch email #{i}")
                .HtmlBody($"<h1>Email #{i}</h1>")
                .BuildAsync(stoppingToken);

            emails.Add(msg);
        }

        var result = await batchSender.SendBatchAsync(emails,
            new BatchSendOptions { MaxConcurrency = 3, DelayMs = 100 },
            stoppingToken);

        logger.LogInformation("Sent {SentCount}/{TotalCount} emails, {FailedCount} failed",
            result.SentCount, result.TotalCount, result.FailedCount);
    }
}
