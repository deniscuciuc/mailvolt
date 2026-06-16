# MailVolt

> Modern .NET email library. Drop-in replacement for FluentEmail.

## Why MailVolt?

| FluentEmail Problem | MailVolt Solution |
|---|---|
| Abandoned since 2022 | Actively maintained |
| No async API | Async-only from day one |
| No DI-first design | DI-first with `MailVoltBuilder` |
| No batch sending | `IBatchEmailSender` with concurrency control |
| No SendGrid inline images | Supported |
| No Mailgun ReplyTo | Fixed |
| No test helpers | `InMemorySender` + FluentAssertions |
| RazorLight dependency | Native ASP.NET Core Razor |

## Quick Start (AutoConfigure)

```
dotnet add package MailVolt.AutoConfigure
```

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com", "DisplayName": "My App" },
    "Transport": "Smtp",
    "Templates": "Razor",
    "Smtp": {
      "Host": "smtp.mailtrap.io",
      "Port": 587,
      "Username": "USER",
      "Password": "PASS"
    }
  }
}
```

```csharp
// Program.cs — one line, everything from config
builder.Services.AddMailVolt(builder.Configuration);
```

Switch providers without changing code — just update `Transport` in config and add the matching section.
Supports: `Smtp` · `SendGrid` · `Mailgun` · `Resend` · `Postmark` · `Azure` · `Brevo` · `AwsSes` · `InMemory`

## Quick Start

```csharp
// 1. Install
// dotnet add package MailVolt.Core
// dotnet add package MailVolt.Transport.Smtp

// 2. Register
services.AddMailVolt()
    .UseSmtpTransport(options =>
    {
        options.Host = "smtp.example.com";
        options.Username = "user";
        options.Password = "pass";
    });

// 3. Send
var builder = services.GetRequiredService<IEmailBuilder>();
var result = await builder
    .From("sender@example.com")
    .To("recipient@example.com")
    .Subject("Hello from MailVolt!")
    .HtmlBody("<h1>Welcome!</h1>")
    .SendAsync();
```

## Installation

| Package | Command |
|---|---|
| Core | `dotnet add package MailVolt.Core` |
| `MailVolt.AutoConfigure` | Zero-code setup — configure everything via appsettings.json |
| Testing | `dotnet add package MailVolt.Testing` |
| Templates: Razor | `dotnet add package MailVolt.Templates.Razor` |
| Templates: Liquid | `dotnet add package MailVolt.Templates.Liquid` |
| Templates: Handlebars | `dotnet add package MailVolt.Templates.Handlebars` |

## Senders

| Provider | Package | Docs |
|---|---|---|
| SMTP (MailKit) | `MailVolt.Transport.Smtp` | [docs](./docs/senders/smtp.md) |
| SendGrid | `MailVolt.Transport.SendGrid` | [docs](./docs/senders/sendgrid.md) |
| Mailgun | `MailVolt.Transport.Mailgun` | [docs](./docs/senders/mailgun.md) |
| Resend | `MailVolt.Transport.Resend` | [docs](./docs/senders/resend.md) |
| Postmark | `MailVolt.Transport.Postmark` | [docs](./docs/senders/postmark.md) |
| Azure Email | `MailVolt.Transport.AzureEmail` | [docs](./docs/senders/azure.md) |
| Brevo | `MailVolt.Transport.Brevo` | [docs](./docs/senders/brevo.md) |
| AWS SES | `MailVolt.Transport.AwsSes` | [docs](./docs/senders/aws-ses.md) |

## Templates

| Engine | Package | Docs |
|---|---|---|
| Razor (.cshtml) | `MailVolt.Templates.Razor` | [docs](./docs/templates/razor.md) |
| Liquid | `MailVolt.Templates.Liquid` | [docs](./docs/templates/liquid.md) |
| Handlebars | `MailVolt.Templates.Handlebars` | [docs](./docs/templates/handlebars.md) |

## Batch Sending

```csharp
var batchSender = services.GetRequiredService<IBatchEmailSender>();

var result = await batchSender.SendBatchAsync(emails, new BatchSendOptions
{
    MaxConcurrency = 10,
    FailureStrategy = FailureStrategy.Continue
});

Console.WriteLine($"Sent {result.SentCount}/{result.TotalCount}");
```

## Testing

```csharp
// Arrange
services.AddMailVolt().UseInMemoryTransport();
var sender = services.GetRequiredService<InMemorySender>();

// Act
await service.SendEmailAsync();

// Assert
sender.Should().HaveCount(1);
sender.Should().ContainEmailTo("user@example.com");
sender.Should().ContainSubject("Welcome!");
```

## Documentation

- [Getting Started](./docs/getting-started.md)
- [Senders](./docs/senders/smtp.md)
- [Templates](./docs/templates/razor.md)
- [Batch Sending](./docs/advanced/batch-sending.md)
- [Resilience](./docs/advanced/resilience.md)
- [Testing](./docs/advanced/testing.md)

## License

[MIT](LICENSE)
