using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Templates.Handlebars.DependencyInjection;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Transport.Mailgun.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        if (dryRun) mv.UseInMemoryTransport();
        else mv.UseMailgunTransport(ctx.Configuration);

        mv.UseHandlebarsTemplates();
    })
    .Build();

var model = new
{
    name = "Denis",
    reset_url = "https://app.example.com/reset?token=abc123xyz",
    expiry_minutes = 30,
    show_warning = true,
    ip = "195.65.12.3"
};

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

var result = await emailBuilder
    .To(new EmailAddress("denis@example.com"))
    .Subject("Reset your password")
    .Tag("transactional")
    .Tag("password-reset")
    .Priority(EmailPriority.High)
    .UsingTemplate("Templates/PasswordReset.hbs", model)
    .SendAsync();

Console.WriteLine(result.IsSuccess ? "✅ Sent" : $"❌ {result.Error}");
