using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Templates.Liquid.DependencyInjection;
using MailVolt.Testing.DependencyInjection;
using MailVolt.Transport.SendGrid.DependencyInjection;
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
        var mv = services.AddMailVolt(opts =>
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!);

        if (dryRun) mv.UseInMemoryTransport();
        else        mv.AddSendGridSender(ctx.Configuration);

        mv.UseLiquidTemplates();
    })
    .Build();

var model = new
{
    order_id     = "ORD-20260616-001",
    customer_name = "Denis",
    item_count   = 3,
    total        = "$149.00",
    items = new[]
    {
        new { name = "MailVolt T-Shirt", qty = 1, price = "$29.00" },
        new { name = "Dev Stickers Pack", qty = 2, price = "$10.00 each" },
        new { name = "Coffee Mug",        qty = 1, price = "$19.00" },
    }
};

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

var result = await emailBuilder
    .To(new EmailAddress("customer@example.com", "Denis"))
    .Subject($"Order #{model.order_id} Confirmed!")
    .UsingTemplate("Templates/OrderConfirm.liquid", model)
    .SendAsync();

Console.WriteLine(result.IsSuccess
    ? $"✅ Sent via SendGrid — {result.MessageId}"
    : $"❌ {result.Error}");
