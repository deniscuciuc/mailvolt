using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Templates.Razor.DependencyInjection;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Transport.Smtp.DependencyInjection;
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
        var defaultDisplayName = ctx.Configuration["MailVolt:DefaultFromDisplayName"];

        var builder = services.AddMailVolt(opts =>
        {
            opts.DefaultFromAddress = defaultFrom;
            opts.DefaultFromDisplayName = defaultDisplayName;
        });

        if (dryRun)
        {
            Console.WriteLine("🔵 Dry run — using InMemorySender (no email sent)");
            builder.UseInMemoryTransport();
        }
        else
        {
            builder.UseSmtpTransport(ctx.Configuration);
        }

        builder.UseRazorTemplates();
    })
    .Build();

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

var model = new WelcomeModel("Denis", "Pro", DateTimeOffset.UtcNow);

var result = await emailBuilder
    .To(new EmailAddress("recipient@example.com", "Denis"))
    .Subject("Welcome to MailVolt Demo 🚀")
    .UsingTemplate("Templates/Welcome.cshtml", model)
    .SendAsync();

if (result.IsSuccess)
    Console.WriteLine($"✅ Sent! MessageId: {result.MessageId}");
else
    Console.WriteLine($"❌ Failed: {result.Error}");

public record WelcomeModel(string Name, string Plan, DateTimeOffset SentAt);
