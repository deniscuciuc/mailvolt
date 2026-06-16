# AGENT_PLAN_EXAMPLES.md — MailVolt Examples

> **For AI agents (Claude Code / GitHub Copilot Agent / OpenAI Codex)**
> This plan covers ONLY the `examples/` directory.
> Assumes `MailVolt.*` packages are already built locally (or published to NuGet).
> Each example is a standalone, runnable project — minimal, focused, no shared code between them.

---

## Goal

Build 9 small standalone example projects that demonstrate MailVolt in realistic scenarios.
Each example covers a different combination of transport + template + pattern.
A developer should be able to clone any example, add credentials, and run it in under 2 minutes.

---

## Rules

- ❌ Do NOT share code between examples — each is fully self-contained
- ❌ Do NOT add complexity not needed for the demo point
- ❌ Do NOT use environment-specific hardcoded values — always read from config/env
- ✅ Every example MUST compile and run with `dotnet run`
- ✅ Every example MUST have its own `README.md` explaining what it demos and how to run it
- ✅ Target `net10.0` for all examples
- ✅ Use `C# 14` idioms (extension members, field keyword, etc.)
- ✅ Use `appsettings.json` + env var override for credentials (12-factor)
- ✅ Every example that sends email MUST support a `--dry-run` flag that uses `InMemorySender`

---

## Structure

```
examples/
├── 01-console-smtp-razor/          # Simplest possible: SMTP + Razor, console app
├── 02-console-sendgrid-liquid/     # SendGrid + Liquid templates
├── 03-console-mailgun-handlebars/  # Mailgun + Handlebars templates
├── 04-console-resend-batch/        # Resend + Batch API (bulk sending)
├── 05-minimalapi-smtp/             # ASP.NET Core Minimal API endpoint that sends email
├── 06-worker-scheduler/            # Background Worker: scheduled digest emails
├── 07-inline-images/               # Any transport + inline CID images in HTML
├── 08-multi-provider-fallback/     # Primary + fallback sender (resilience pattern)
├── 09-testing-demo/                # How to test code that sends email (InMemorySender)
└── README.md                       # Index of all examples
```

---

## Shared appsettings pattern (use in ALL examples)

Every example reads credentials from `appsettings.json` (gitignored) with env var override.
Never commit real credentials. Provide `appsettings.example.json` with placeholder values.

```json
// appsettings.example.json — commit this
{
  "MailVolt": {
    "DefaultFromAddress": "noreply@example.com",
    "DefaultFromDisplayName": "MailVolt Demo"
  }
}
```

```json
// appsettings.json — in .gitignore, filled by developer
{
  "MailVolt": {
    "DefaultFromAddress": "you@yourdomain.com",
    "DefaultFromDisplayName": "MailVolt Demo",
    "Smtp": {
      "Host": "smtp.mailtrap.io",
      "Port": 587,
      "Username": "YOUR_USERNAME",
      "Password": "YOUR_PASSWORD"
    }
  }
}
```

`.gitignore` for every example must include `appsettings.json` (not `appsettings.*.json`).

---

## Example 01 — Console · SMTP + Razor

**Path:** `examples/01-console-smtp-razor/`  
**Shows:** The most common combo. Simplest entry point for new users.  
**Sends:** A welcome email with a Razor `.cshtml` template.

### Project file

```xml
<!-- 01-console-smtp-razor.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>14</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MailVolt.Core" Version="*" />
    <PackageReference Include="MailVolt.Transport.Smtp" Version="*" />
    <PackageReference Include="MailVolt.Templates.Razor" Version="*" />
    <PackageReference Include="MailVolt.Testing" Version="*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup>
    <!-- Razor views must be content files -->
    <Content Include="Templates/**/*.cshtml">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="appsettings*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

### Directory layout

```
01-console-smtp-razor/
├── Program.cs
├── Templates/
│   └── Welcome.cshtml
├── appsettings.example.json
├── appsettings.json          ← gitignored
└── README.md
```

### Templates/Welcome.cshtml

```html
@model WelcomeModel
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8"/>
  <style>
    body { font-family: Arial, sans-serif; background: #f5f5f5; padding: 20px; }
    .card { background: white; padding: 32px; border-radius: 8px; max-width: 480px; margin: auto; }
    h1 { color: #1a1a2e; }
    .badge { background: #3B7DD8; color: white; padding: 4px 12px; border-radius: 4px; font-size: 13px; }
  </style>
</head>
<body>
  <div class="card">
    <h1>Welcome, @Model.Name! 👋</h1>
    <p>Thanks for signing up. Your account is ready.</p>
    <p><span class="badge">@Model.Plan</span></p>
    <p style="color:#666; font-size:13px;">Sent at @Model.SentAt.ToString("yyyy-MM-dd HH:mm") UTC</p>
  </div>
</body>
</html>
```

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Testing;
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
        var builder = services.AddMailVolt(opts =>
        {
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!;
            opts.DefaultFromDisplayName = ctx.Configuration["MailVolt:DefaultFromDisplayName"];
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
    .To("recipient@example.com", "Denis")
    .Subject("Welcome to MailVolt Demo 🚀")
    .UsingTemplate("Templates/Welcome.cshtml", model)
    .SendAsync();

if (result.IsSuccess)
    Console.WriteLine($"✅ Sent! MessageId: {result.MessageId}");
else
    Console.WriteLine($"❌ Failed: {result.Error}");

public record WelcomeModel(string Name, string Plan, DateTimeOffset SentAt);
```

### README.md

```markdown
# 01 — Console · SMTP + Razor

Sends a welcome email via SMTP using a Razor `.cshtml` template.

## Setup
1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your SMTP credentials (Mailtrap recommended for testing)

## Run
```bash
dotnet run                  # sends real email
dotnet run -- --dry-run     # prints result, no email sent
```

## What it shows
- SMTP transport with MailKit
- Razor template from file
- `--dry-run` with InMemorySender
```

---

## Example 02 — Console · SendGrid + Liquid

**Path:** `examples/02-console-sendgrid-liquid/`  
**Shows:** Cloud provider (SendGrid) + Liquid templates (safer for user-generated content).  
**Sends:** An order confirmation email.

### Directory layout

```
02-console-sendgrid-liquid/
├── Program.cs
├── Templates/
│   └── OrderConfirm.liquid
├── appsettings.example.json
├── appsettings.json          ← gitignored
└── README.md
```

### Templates/OrderConfirm.liquid

```html
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"/></head>
<body style="font-family: Arial, sans-serif; padding: 20px;">
  <h2>Order #{{ order_id }} Confirmed ✅</h2>
  <p>Hi {{ customer_name }},</p>
  <p>Your order of <strong>{{ item_count }} item(s)</strong> totalling
     <strong>{{ total }}</strong> is confirmed.</p>
  <table border="1" cellpadding="8" style="border-collapse:collapse; width:100%;">
    <tr style="background:#f0f0f0;">
      <th>Item</th><th>Qty</th><th>Price</th>
    </tr>
    {% for item in items %}
    <tr>
      <td>{{ item.name }}</td>
      <td>{{ item.qty }}</td>
      <td>{{ item.price }}</td>
    </tr>
    {% endfor %}
  </table>
  <p style="color:#888; font-size:12px;">Thank you for your purchase!</p>
</body>
</html>
```

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Testing;
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
        else        mv.UseSendGridTransport(ctx.Configuration);

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
    .To("customer@example.com", "Denis")
    .Subject($"Order #{model.order_id} Confirmed!")
    .UsingTemplate("Templates/OrderConfirm.liquid", model)
    .SendAsync();

Console.WriteLine(result.IsSuccess
    ? $"✅ Sent via SendGrid — {result.MessageId}"
    : $"❌ {result.Error}");
```

### appsettings.example.json

```json
{
  "MailVolt": {
    "DefaultFromAddress": "noreply@yourdomain.com",
    "SendGrid": {
      "ApiKey": "SG.YOUR_KEY_HERE"
    }
  }
}
```

---

## Example 03 — Console · Mailgun + Handlebars

**Path:** `examples/03-console-mailgun-handlebars/`  
**Shows:** Mailgun transport + Handlebars templates + tags for categorisation.  
**Sends:** A password reset email with a one-time link.

### Templates/PasswordReset.hbs

```html
<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; padding: 24px;">
  <h2>🔐 Password Reset</h2>
  <p>Hi {{name}},</p>
  <p>You requested a password reset. Click below — this link expires in {{expiry_minutes}} minutes.</p>
  <p>
    <a href="{{reset_url}}"
       style="background:#3B7DD8; color:white; padding:12px 24px;
              text-decoration:none; border-radius:4px; display:inline-block;">
      Reset My Password
    </a>
  </p>
  {{#if show_warning}}
  <p style="color:#c05621; font-size:13px;">
    ⚠️ If you didn't request this, ignore this email and your password won't change.
  </p>
  {{/if}}
  <p style="color:#999; font-size:12px;">Request from IP: {{ip}}</p>
</body>
</html>
```

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Testing;
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
        else        mv.UseMailgunTransport(ctx.Configuration);

        mv.UseHandlebarsTemplates();
    })
    .Build();

var model = new
{
    name            = "Denis",
    reset_url       = "https://app.example.com/reset?token=abc123xyz",
    expiry_minutes  = 30,
    show_warning    = true,
    ip              = "195.65.12.3"
};

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

var result = await emailBuilder
    .To("denis@example.com")
    .Subject("Reset your password")
    .Tag("transactional", "password-reset")
    .Priority(MailVolt.Core.Models.EmailPriority.High)
    .UsingTemplate("Templates/PasswordReset.hbs", model)
    .SendAsync();

Console.WriteLine(result.IsSuccess ? "✅ Sent" : $"❌ {result.Error}");
```

---

## Example 04 — Console · Resend + Batch

**Path:** `examples/04-console-resend-batch/`  
**Shows:** Bulk/batch email sending — newsletter to a list of recipients.  
**Sends:** 20 newsletter emails via `IBatchEmailSender` with concurrency throttle.

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Testing;
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
        var mv = services.AddMailVault(opts =>
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!);

        if (dryRun) mv.UseInMemoryTransport();
        else        mv.UseResendTransport(ctx.Configuration);

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
        .To(sub.Email, sub.Name)
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
    FailureStrategy = BatchFailureStrategy.ContinueOnFailure
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
```

### Templates/Newsletter.liquid

```html
<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; padding: 20px; max-width: 600px; margin: auto;">
  <h1 style="color:#1a1a2e;">📰 MailVolt Monthly</h1>
  <p>Hi {{ Name }},</p>
  {% if Plan == "Pro" %}
  <p>🌟 <strong>Pro member exclusive:</strong> Early access to MailVolt 2.0 is now open!</p>
  {% else %}
  <p>Upgrade to Pro to get early access to MailVolt 2.0.</p>
  {% endif %}
  <p>Thanks for being part of the community.</p>
  <p style="color:#999; font-size:12px;">Unsubscribe | Update preferences</p>
</body>
</html>
```

---

## Example 05 — ASP.NET Core Minimal API · SMTP

**Path:** `examples/05-minimalapi-smtp/`  
**Shows:** Sending email from an HTTP endpoint — the classic web app use case.  
**Sends:** Contact form submission notification.

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMailVolt(opts =>
    {
        opts.DefaultFromAddress     = builder.Configuration["MailVolt:DefaultFromAddress"]!;
        opts.DefaultFromDisplayName = "MailVolt API Demo";
    })
    .UseSmtpTransport(builder.Configuration)
    .UseRazorTemplates();

var app = builder.Build();

// POST /contact  { name, email, message }
app.MapPost("/contact", async (ContactRequest req, IEmailBuilder emailBuilder) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Message))
        return Results.BadRequest("email and message are required");

    var result = await emailBuilder
        .To("admin@example.com", "Site Admin")
        .ReplyTo(req.Email, req.Name)
        .Subject($"📩 Contact form: {req.Name}")
        .Body($"""
            <h3>New contact form submission</h3>
            <p><strong>From:</strong> {req.Name} ({req.Email})</p>
            <p><strong>Message:</strong></p>
            <p>{req.Message}</p>
            """, isHtml: true)
        .SendAsync();

    return result.IsSuccess
        ? Results.Ok(new { sent = true, messageId = result.MessageId })
        : Results.Problem(result.Error);
})
.WithName("SendContact")
.WithOpenApi();

// GET /health
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.Run();

public record ContactRequest(string Name, string Email, string Message);
```

### README.md

```markdown
# 05 — Minimal API · SMTP

HTTP endpoint that sends an email on POST /contact.

## Run
```bash
dotnet run
curl -X POST http://localhost:5000/contact \
  -H "Content-Type: application/json" \
  -d '{"name":"Denis","email":"d@example.com","message":"Hello!"}'
```
```

---

## Example 06 — Worker Service · Scheduled Digest

**Path:** `examples/06-worker-scheduler/`  
**Shows:** Background worker that sends daily digest emails on a schedule.  
**Sends:** Daily digest to a list of subscribers every 24h (or on demand via arg).

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Testing;
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
        else        mv.UseSmtpTransport(ctx.Configuration);

        mv.UseLiquidTemplates();

        services.AddHostedService<DigestWorker>();
    })
    .Build();

await host.RunAsync();
```

### DigestWorker.cs

```csharp
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
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

        var subscribers = GetSubscribers();     // replace with real DB query
        var model = new DigestModel(
            Date:     DateOnly.FromDateTime(DateTime.Today),
            Articles: ["New in .NET 10", "MailVolt 1.0 released", "C# 14 tips & tricks"]
        );

        var messages = new List<EmailMessage>();
        foreach (var sub in subscribers)
        {
            var msg = await emailBuilder
                .To(sub.Email, sub.Name)
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
```

### Templates/Digest.liquid

```html
<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 560px; margin: auto; padding: 24px;">
  <h2>📰 Daily Digest — {{ Date }}</h2>
  <p>Hi {{ SubscriberName }},</p>
  <p>Here's what's happening today:</p>
  <ul>
    {% for article in Articles %}
    <li>{{ article }}</li>
    {% endfor %}
  </ul>
  <p style="color:#999; font-size:12px;">You're receiving this because you subscribed to daily digests.</p>
</body>
</html>
```

---

## Example 07 — Console · Inline Images (CID)

**Path:** `examples/07-inline-images/`  
**Shows:** Embedding images directly in the HTML email body using CID attachments.  
**Sends:** An invoice email with a company logo embedded inline.

### Directory layout

```
07-inline-images/
├── Program.cs
├── Templates/
│   └── Invoice.cshtml
├── Assets/
│   └── logo.png          ← any PNG, ~100x40px
├── appsettings.example.json
└── README.md
```

### Templates/Invoice.cshtml

```html
@model InvoiceModel
<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; padding: 24px;">
  <!-- Inline image: src="cid:{ContentId}" -->
  <img src="cid:company-logo" alt="Company Logo" style="height:40px; margin-bottom:16px;"/>
  <hr/>
  <h2>Invoice #@Model.InvoiceNumber</h2>
  <p>Bill to: <strong>@Model.CustomerName</strong></p>
  <table style="width:100%; border-collapse:collapse;">
    <tr style="background:#f0f0f0;">
      <th style="padding:8px; text-align:left;">Description</th>
      <th style="padding:8px; text-align:right;">Amount</th>
    </tr>
    @foreach (var line in Model.Lines)
    {
      <tr>
        <td style="padding:8px; border-top:1px solid #eee;">@line.Description</td>
        <td style="padding:8px; border-top:1px solid #eee; text-align:right;">@line.Amount</td>
      </tr>
    }
    <tr style="font-weight:bold;">
      <td style="padding:8px; border-top:2px solid #333;">Total</td>
      <td style="padding:8px; border-top:2px solid #333; text-align:right;">@Model.Total</td>
    </tr>
  </table>
  <p style="color:#999; font-size:12px; margin-top:24px;">Due date: @Model.DueDate.ToString("MMMM dd, yyyy")</p>
</body>
</html>
```

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Testing;
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
        var mv = services.AddMailVault(opts =>
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!);

        if (dryRun) mv.UseInMemoryTransport();
        else        mv.UseSmtpTransport(ctx.Configuration);

        mv.UseRazorTemplates();
    })
    .Build();

var model = new InvoiceModel(
    InvoiceNumber: "INV-2026-0042",
    CustomerName:  "Denis Cuciuc",
    DueDate:       DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
    Lines:
    [
        new("MailVolt Pro License · 1 year", "$199.00"),
        new("Priority Support",              "$49.00"),
    ],
    Total: "$248.00"
);

var emailBuilder = host.Services.GetRequiredService<IEmailBuilder>();

var result = await emailBuilder
    .To("denis@example.com", "Denis Cuciuc")
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
```

---

## Example 08 — Console · Multi-Provider Fallback

**Path:** `examples/08-multi-provider-fallback/`  
**Shows:** Primary sender (Resend) + automatic fallback to SMTP if primary fails.  
**Pattern:** Resilience — useful in production when a provider has downtime.

### FallbackSender.cs

```csharp
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tries primarySender first. If it fails, logs a warning and tries fallbackSender.
/// </summary>
public sealed class FallbackSender(
    ISender primarySender,
    ISender fallbackSender,
    ILogger<FallbackSender> logger) : ISender
{
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken ct = default)
    {
        var primary = await primarySender.SendAsync(email, ct);

        if (primary.IsSuccess)
            return primary;

        logger.LogWarning(
            "Primary sender failed ({Error}). Trying fallback...", primary.Error);

        var fallback = await fallbackSender.SendAsync(email, ct);

        if (fallback.IsSuccess)
            logger.LogInformation("Fallback sender succeeded. MessageId: {Id}", fallback.MessageId);
        else
            logger.LogError("Both primary and fallback failed. Last error: {Error}", fallback.Error);

        return fallback;
    }
}
```

### Program.cs

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Resend;
using MailVolt.Transport.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg => cfg
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables("MAILVOLT_"))
    .ConfigureServices((ctx, services) =>
    {
        services.AddMailVault(opts =>
            opts.DefaultFromAddress = ctx.Configuration["MailVolt:DefaultFromAddress"]!);

        // Register both senders by their concrete type (not ISender)
        services.AddTransient<ResendSender>(sp =>
            ResendSender.Create(ctx.Configuration, sp));

        services.AddTransient<SmtpSender>(sp =>
            SmtpSender.Create(ctx.Configuration, sp));

        // Register the fallback wrapper as the ISender
        services.AddTransient<ISender, FallbackSender>(sp => new FallbackSender(
            primarySender:  sp.GetRequiredService<ResendSender>(),
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
    .Body("<h2>This was sent with automatic fallback!</h2>", isHtml: true)
    .SendAsync();

Console.WriteLine(result.IsSuccess ? $"✅ {result.MessageId}" : $"❌ {result.Error}");
```

### appsettings.example.json

```json
{
  "MailVolt": {
    "DefaultFromAddress": "noreply@yourdomain.com",
    "Resend": {
      "ApiKey": "re_YOUR_KEY"
    },
    "Smtp": {
      "Host": "smtp.mailtrap.io",
      "Port": 587,
      "Username": "YOUR_USERNAME",
      "Password": "YOUR_PASSWORD"
    }
  }
}
```

---

## Example 09 — Testing Demo

**Path:** `examples/09-testing-demo/`  
**Shows:** How to write unit tests for code that sends email using `InMemorySender`.  
**No real email sent.** Pure xUnit project, runs with `dotnet test`.

### Project structure

```
09-testing-demo/
├── src/
│   ├── UserService.cs         ← business logic that uses IEmailBuilder
│   └── 09-testing-demo.csproj
├── tests/
│   ├── UserServiceTests.cs
│   └── 09-testing-demo.Tests.csproj
└── README.md
```

### src/UserService.cs

```csharp
using MailVolt.Core.Interfaces;

/// <summary>
/// Example service that sends email as a side effect of business logic.
/// This is the code you want to test without hitting a real SMTP server.
/// </summary>
public sealed class UserService(IEmailBuilder emailBuilder)
{
    public async Task<bool> RegisterAsync(string email, string name, CancellationToken ct = default)
    {
        // ... save to DB ...

        var result = await emailBuilder
            .To(email, name)
            .Subject("Welcome!")
            .Body($"<h1>Hi {name}, welcome aboard!</h1>", isHtml: true)
            .SendAsync(ct);

        return result.IsSuccess;
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken ct = default)
    {
        await emailBuilder
            .To(email)
            .Subject("Reset your password")
            .Priority(MailVolt.Core.Models.EmailPriority.High)
            .Body($"<p>Click to reset: <a href='{resetLink}'>here</a></p>", isHtml: true)
            .SendAsync(ct);
    }
}
```

### tests/UserServiceTests.cs

```csharp
using FluentAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Testing;
using Microsoft.Extensions.DependencyInjection;

public sealed class UserServiceTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly InMemorySender  _sender;
    private readonly UserService     _sut;

    public UserServiceTests()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();

        sc.AddMailVault(opts => opts.DefaultFromAddress = "noreply@test.com")
          .UseInMemoryTransport();

        // UserService depends on IEmailBuilder, which is Transient — resolved normally
        sc.AddTransient<UserService>();

        _services = sc.BuildServiceProvider();
        _sender   = _services.GetRequiredService<InMemorySender>();
        _sut      = _services.GetRequiredService<UserService>();
    }

    [Fact]
    public async Task Register_SendsWelcomeEmail_ToCorrectAddress()
    {
        // Act
        var success = await _sut.RegisterAsync("denis@example.com", "Denis");

        // Assert
        success.Should().BeTrue();
        _sender.Should().HaveCount(1)
                        .And.ContainEmailTo("denis@example.com")
                        .And.ContainSubject("Welcome");
    }

    [Fact]
    public async Task Register_SendsExactlyOneEmail()
    {
        await _sut.RegisterAsync("alice@example.com", "Alice");
        await _sut.RegisterAsync("bob@example.com",   "Bob");

        _sender.SentEmails.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendPasswordReset_UsesHighPriority()
    {
        await _sut.SendPasswordResetAsync("user@example.com", "https://app/reset?token=abc");

        var sent = _sender.SentEmails.Single();
        sent.Email.Priority.Should().Be(EmailPriority.High);
        sent.Email.HtmlBody.Should().Contain("abc");
    }

    [Fact]
    public async Task NoSideEffects_BetweenTests()
    {
        // Each test creates fresh services — InMemorySender starts empty
        _sender.Should().HaveNoEmailsSent();
    }

    public void Dispose() => _services.Dispose();
}
```

### README.md

```markdown
# 09 — Testing Demo

Shows how to unit-test code that sends email using `InMemorySender` — no real emails, no network.

## Run tests
```bash
cd tests
dotnet test
```

## Key pattern
1. Register `.UseInMemoryTransport()` in test DI setup
2. Resolve `InMemorySender` as singleton — it captures all sent emails in memory
3. Use `_sender.Should().ContainEmailTo(...)` FluentAssertions extensions
```

---

## Root examples/README.md

```markdown
# MailVolt Examples

Quick-start examples showing different combinations of transports and templates.
Each example is fully standalone — just add your credentials and `dotnet run`.

| # | Example | Transport | Template | Pattern |
|---|---------|-----------|----------|---------|
| 01 | [Console · SMTP + Razor](./01-console-smtp-razor/) | SMTP | Razor | Welcome email |
| 02 | [Console · SendGrid + Liquid](./02-console-sendgrid-liquid/) | SendGrid | Liquid | Order confirmation |
| 03 | [Console · Mailgun + Handlebars](./03-console-mailgun-handlebars/) | Mailgun | Handlebars | Password reset |
| 04 | [Console · Resend + Batch](./04-console-resend-batch/) | Resend | Liquid | Newsletter bulk send |
| 05 | [Minimal API · SMTP](./05-minimalapi-smtp/) | SMTP | Inline HTML | Contact form endpoint |
| 06 | [Worker · Scheduled Digest](./06-worker-scheduler/) | SMTP | Liquid | Background daily digest |
| 07 | [Console · Inline Images](./07-inline-images/) | SMTP | Razor | Invoice with CID logo |
| 08 | [Console · Multi-Provider Fallback](./08-multi-provider-fallback/) | Resend → SMTP | Inline HTML | Resilience pattern |
| 09 | [Testing Demo](./09-testing-demo/) | InMemory | None | Unit testing with InMemorySender |

## Quick start (any example)

```bash
# 1. Copy config
cp appsettings.example.json appsettings.json

# 2. Fill in your credentials (use Mailtrap for safe testing)
# https://mailtrap.io/register/signup

# 3. Run
dotnet run                  # sends real email
dotnet run -- --dry-run     # InMemorySender — no email sent, prints result
```

## Recommended order for learning

1. Start with **01** (simplest)
2. **09** to understand testing
3. **04** for batch sending
4. **08** for production resilience pattern
```

---

## Implementation Order

```
Step 1 → Create examples/ directory + root README.md
Step 2 → Example 01 (console + SMTP + Razor) — verify dotnet run works
Step 3 → Example 09 (testing) — verify dotnet test passes
Step 4 → Example 02 (SendGrid + Liquid)
Step 5 → Example 03 (Mailgun + Handlebars)
Step 6 → Example 04 (Resend + Batch)
Step 7 → Example 05 (Minimal API)
Step 8 → Example 06 (Worker + Scheduler)
Step 9 → Example 07 (Inline Images)
Step 10 → Example 08 (Multi-provider Fallback)
Step 11 → Root examples/README.md table
```

After each example: `dotnet build` MUST pass. For 09: `dotnet test` MUST pass.

---

## Done Checklist

- [ ] `examples/README.md` with full index table
- [ ] `01-console-smtp-razor` — builds, `--dry-run` works
- [ ] `02-console-sendgrid-liquid` — builds, `--dry-run` works
- [ ] `03-console-mailgun-handlebars` — builds, `--dry-run` works
- [ ] `04-console-resend-batch` — builds, `--dry-run` works, batch result printed
- [ ] `05-minimalapi-smtp` — builds, GET /health returns 200
- [ ] `06-worker-scheduler` — builds, starts without crash
- [ ] `07-inline-images` — builds, `--dry-run` works
- [ ] `08-multi-provider-fallback` — builds, fallback logic compiles
- [ ] `09-testing-demo` — `dotnet test` passes, all 4 tests green
- [ ] Every example has its own `README.md`
- [ ] Every example has `appsettings.example.json` (no real credentials)
- [ ] `appsettings.json` is in `.gitignore` of each example
