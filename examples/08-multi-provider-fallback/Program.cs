using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Transport.Resend;
using MailVolt.Transport.Resend.DependencyInjection;
using MailVolt.Transport.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var dryRun = args.Contains("--dry-run");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables())
    .ConfigureServices((ctx, services) =>
    {
        var defaultFrom = ctx.Configuration["MailVolt:DefaultFromAddress"] ?? "dryrun@example.com";
        var mv = services.AddMailVolt(opts =>
            opts.DefaultFromAddress = defaultFrom);

        if (dryRun)
        {
            Console.WriteLine("🔵 Dry run — using InMemorySender (no email sent)");
            mv.UseInMemoryTransport();
            return;
        }

        // Register Resend as the primary transport
        mv.UseResend();

        // Register SMTP as the fallback transport
        services.AddOptions<SmtpSenderOptions>()
            .BindConfiguration(SmtpSenderOptions.SectionName);
        services.AddTransient<SmtpSender>();

        // Register the fallback wrapper as ISender (overrides the Resend ISender registration)
        services.AddTransient<ISender>(sp => new FallbackSender(
            primarySender: sp.GetRequiredService<IResendSender>(),
            fallbackSender: sp.GetRequiredService<SmtpSender>(),
            logger: sp.GetRequiredService<ILogger<FallbackSender>>()
        ));

        // Simulate primary failure for demo:
        // Set MailVolt__Resend__ApiKey=INVALID_KEY to force fallback
    })
    .Build();

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

Console.WriteLine("Sending via Resend (primary) → SMTP (fallback)...");

var result = await emailBuilder
    .To("recipient@example.com")
    .Subject("Fallback pattern demo")
    .HtmlBody("<h2>This was sent with automatic fallback!</h2>")
    .SendAsync();

Console.WriteLine(result.IsSuccess ? $"✅ {result.MessageId}" : $"❌ {result.Error}");
