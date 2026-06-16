using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Resend;
using MailVolt.Transport.Resend.DependencyInjection;
using MailVolt.Transport.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables("MAILVOLT_"))
    .ConfigureServices((ctx, services) =>
    {
        var mv = services.AddMailVolt(opts =>
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!);

        // Register Resend as the primary transport
        mv.UseResend();

        // Register SMTP as the fallback transport
        services.AddOptions<SmtpSenderOptions>()
            .BindConfiguration(SmtpSenderOptions.SectionName);
        services.AddTransient<SmtpSender>();

        // Register the fallback wrapper as ISender (overrides the Resend ISender registration)
        services.AddTransient<ISender>(sp => new FallbackSender(
            primarySender:  sp.GetRequiredService<IResendSender>(),
            fallbackSender: sp.GetRequiredService<SmtpSender>(),
            logger:         sp.GetRequiredService<ILogger<FallbackSender>>()
        ));

        // Simulate primary failure for demo:
        // Set MAILVOLT_RESEND__APIKEY=INVALID_KEY to force fallback
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
