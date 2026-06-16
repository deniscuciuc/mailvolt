using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Templates.Liquid.DependencyInjection;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Transport.Resend.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var dryRun = args.Contains("--dry-run");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables("MAILVOLT_"))
    .ConfigureServices((ctx, services) =>
    {
        var defaultFrom = ctx.Configuration["MailVolt:DefaultFromAddress"] ?? "dryrun@example.com";

        var mv = services.AddMailVolt(opts =>
            opts.DefaultFromAddress = defaultFrom);

        if (dryRun) mv.UseInMemoryTransport();
        else        mv.UseResend(ctx.Configuration.GetSection("MailVolt:Resend"));

        mv.UseLiquidTemplates();
    })
    .Build();

// Fake subscriber list — replace with DB query in real app
var subscribers = Enumerable.Range(1, 20).Select(i => new Subscriber(
    Email: $"user{i}@example.com",
    Name: $"User {i}",
    Plan: i % 3 == 0 ? "Pro" : "Free"
)).ToList();

Console.WriteLine($"📧 Sending newsletter to {subscribers.Count} subscribers...");

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

// Build all messages up front
var messages = new List<EmailMessage>();
foreach (var sub in subscribers)
{
    var msg = await emailBuilder
        .To(new EmailAddress(sub.Email, sub.Name))
        .Subject("📰 MailVolt Monthly — June 2026")
        .UsingTemplate("Templates/Newsletter.liquid", new { sub.Name, sub.Plan })
        .BuildAsync();                     // BuildAsync, not SendAsync — batch sends later
    messages.Add(msg);
}

// Send as batch
var batchSender = host.Services.GetRequiredService<IBatchEmailSender>();

var result = await batchSender.SendBatchAsync(messages, new BatchSendOptions
{
    MaxConcurrency = 5,       // 5 concurrent sends max
    DelayMs        = 100,     // 100ms between sends (rate limit respect)
    FailureStrategy = FailureStrategy.Continue
});

Console.WriteLine($"""
✅ Sent:   {result.SentCount}
❌ Failed: {result.FailedCount}
📊 Total:  {result.TotalCount}
""");

if (result.HasFailures)
{
    foreach (var (email, res) in result.Results.Where(r => r.Result.IsFailure))
        Console.WriteLine($"  Failed → {email.To[0].Address}: {res.Error}");
}

public record Subscriber(string Email, string Name, string Plan);
