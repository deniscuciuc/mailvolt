using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Templates.Razor.DependencyInjection;
using MailVolt.Transport.Smtp.DependencyInjection;
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
        else mv.UseSmtpTransport(ctx.Configuration);

        mv.UseRazorTemplates();
    })
    .Build();

var model = new InvoiceModel(
    InvoiceNumber: "INV-2026-0042",
    CustomerName: "Denis Cuciuc",
    DueDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
    Lines:
    [
        new("MailVolt Pro License · 1 year", "$199.00"),
        new("Priority Support",              "$49.00"),
    ],
    Total: "$248.00"
);

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

var result = await emailBuilder
    .To(new EmailAddress("denis@example.com", "Denis Cuciuc"))
    .Subject($"Invoice {model.InvoiceNumber}")
    .Attach(a => a
        .FromFile("Assets/logo.png")
        .AsInlineImage(contentId: "company-logo"))  // ← CID used in <img src="cid:company-logo">
    .UsingTemplate("Templates/Invoice.cshtml", model)
    .SendAsync();

Console.WriteLine(result.IsSuccess
    ? $"✅ Invoice sent with inline logo! MessageId: {result.MessageId}"
    : $"❌ Failed: {result.Error}");

public record InvoiceLine(string Description, string Amount);
public record InvoiceModel(
    string InvoiceNumber,
    string CustomerName,
    DateOnly DueDate,
    IReadOnlyList<InvoiceLine> Lines,
    string Total);
