# MailVolt

[![CI](https://github.com/deniscuciuc/mailvolt/actions/workflows/ci.yml/badge.svg)](https://github.com/deniscuciuc/mailvolt/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MailVolt.Core.svg?label=MailVolt.Core)](https://www.nuget.org/packages/MailVolt.Core/)
[![NuGet](https://img.shields.io/nuget/v/MailVolt.AutoConfigure.svg?label=MailVolt.AutoConfigure)](https://www.nuget.org/packages/MailVolt.AutoConfigure/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Codecov](https://codecov.io/gh/deniscuciuc/mailvolt/branch/main/graph/badge.svg)](https://codecov.io/gh/deniscuciuc/mailvolt)

> Modern .NET email library. Drop-in replacement for FluentEmail.

## Why MailVolt?

| FluentEmail | MailVolt |
|---|---|
| Unmaintained since 2022 | Actively maintained |
| Sync API with async wrappers | Async-only from day one |
| Static `Email.From(...)` entry point | DI-first with `MailVoltBuilder` |
| No batch sending | `IBatchEmailSender` with concurrency control and rate limiting |
| No SendGrid inline images | Supported |
| Mailgun ignores `ReplyTo` | Fixed |
| No test helpers | `InMemorySender` plus fluent assertions |
| Razor via RazorLight | Native ASP.NET Core Razor |

See [Migrating from FluentEmail](./docs/migrating-from-fluentemail.md).

Targets **net8.0**, **net9.0** and **net10.0**. Building from source needs the **.NET 10 SDK**
(the DI extensions use C# 14 extension members); consuming the packages does not.

## Quick start — zero code

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
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "USER",
      "Password": "PASS"
    }
  }
}
```

```csharp
// Program.cs — everything comes from configuration
builder.Services.AddMailVolt(builder.Configuration);
```

Switch providers by changing `Transport` and adding the matching section — no code change.
Supports `Smtp` · `SendGrid` · `Mailgun` · `Resend` · `Postmark` · `Azure` · `Brevo` ·
`AwsSes` · `InMemory`. See [docs/autoconfigure.md](./docs/autoconfigure.md) for every option.

## Quick start — explicit registration

```
dotnet add package MailVolt.Core
dotnet add package MailVolt.Transport.Smtp
```

```csharp
using MailVolt.Core.DependencyInjection;

// One using covers AddMailVolt and every transport and template engine.
builder.Services.AddMailVolt()
    .UseSmtpTransport(options =>
    {
        options.Host = "smtp.example.com";
        options.Username = "user";
        options.Password = "pass";
    });
```

```csharp
using MailVolt.Core.Interfaces;

public sealed class WelcomeService(IEmailBuilder email)
{
    public async Task SendAsync(string recipient, CancellationToken cancellationToken)
    {
        var result = await email
            .From("sender@example.com")
            .To(recipient)
            .Subject("Hello from MailVolt!")
            .HtmlBody("<h1>Welcome!</h1>")
            .SendAsync(cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error);
        }
    }
}
```

`IEmailBuilder` is registered transient, so inject a fresh one per send. Do not hold one and
reuse it across messages — see [docs/troubleshooting.md](./docs/troubleshooting.md).

## Packages

| Package | Purpose |
|---|---|
| [`MailVolt.Core`](https://www.nuget.org/packages/MailVolt.Core/) | Abstractions, fluent builder, batch sender, in-memory transport |
| [`MailVolt.AutoConfigure`](https://www.nuget.org/packages/MailVolt.AutoConfigure/) | Zero-code setup from `appsettings.json` |
| [`MailVolt.Testing`](https://www.nuget.org/packages/MailVolt.Testing/) | AwesomeAssertions extensions for asserting on sent mail |

Transports: `MailVolt.Transport.{Smtp,SendGrid,Mailgun,Resend,Postmark,AzureEmail,Brevo,AwsSes}`.
Templates: `MailVolt.Templates.{Razor,Liquid,Handlebars}`.

## Transports

| Provider | Package | Registration | Docs |
|---|---|---|---|
| SMTP (MailKit) | `MailVolt.Transport.Smtp` | `UseSmtpTransport` | [docs](./docs/senders/smtp.md) |
| SendGrid | `MailVolt.Transport.SendGrid` | `UseSendGridTransport` | [docs](./docs/senders/sendgrid.md) |
| Mailgun | `MailVolt.Transport.Mailgun` | `UseMailgunTransport` | [docs](./docs/senders/mailgun.md) |
| Resend | `MailVolt.Transport.Resend` | `UseResendTransport` | [docs](./docs/senders/resend.md) |
| Postmark | `MailVolt.Transport.Postmark` | `UsePostmarkTransport` | [docs](./docs/senders/postmark.md) |
| Azure Email | `MailVolt.Transport.AzureEmail` | `UseAzureEmailTransport` | [docs](./docs/senders/azure.md) |
| Brevo | `MailVolt.Transport.Brevo` | `UseBrevoTransport` | [docs](./docs/senders/brevo.md) |
| AWS SES | `MailVolt.Transport.AwsSes` | `UseAwsSesTransport` | [docs](./docs/senders/aws-ses.md) |
| In-memory | `MailVolt.Core` | `UseInMemoryTransport` | [docs](./docs/advanced/testing.md) |

## Templates

| Engine | Package | Registration | Docs |
|---|---|---|---|
| Razor (`.cshtml`) | `MailVolt.Templates.Razor` | `UseRazorTemplates` | [docs](./docs/templates/razor.md) |
| Liquid | `MailVolt.Templates.Liquid` | `UseLiquidTemplates` | [docs](./docs/templates/liquid.md) |
| Handlebars | `MailVolt.Templates.Handlebars` | `UseHandlebarsTemplates` | [docs](./docs/templates/handlebars.md) |

## Batch sending

```csharp
using MailVolt.Core.Interfaces;

public sealed class DigestService(IBatchEmailSender batchSender)
{
    public async Task<BatchEmailResult> SendAsync(
        IReadOnlyList<EmailMessage> emails,
        CancellationToken cancellationToken)
    {
        return await batchSender.SendBatchAsync(emails, new BatchSendOptions(
            MaxConcurrency: 10,
            DelayMs: 200,
            FailureStrategy: FailureStrategy.Continue), cancellationToken);
    }
}
```

`SentCount`, `FailedCount` and `SkippedCount` always sum to `TotalCount`.
See [docs/advanced/batch-sending.md](./docs/advanced/batch-sending.md).

## Testing

```
dotnet add package MailVolt.Testing
```

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Transports;
using MailVolt.Testing;

var services = new ServiceCollection();
services.AddMailVolt().UseInMemoryTransport();
await using var provider = services.BuildServiceProvider();

var sender = provider.GetRequiredService<InMemorySender>();

await provider.GetRequiredService<WelcomeService>().SendAsync("user@example.com", default);

sender.Should()
    .HaveCount(1)
    .ContainEmailTo("user@example.com")
    .ContainSubject("Welcome!");
```

See [docs/advanced/testing.md](./docs/advanced/testing.md).

## Documentation

- [Getting started](./docs/getting-started.md)
- [Configuration reference](./docs/autoconfigure.md)
- [Migrating from FluentEmail](./docs/migrating-from-fluentemail.md)
- [Troubleshooting](./docs/troubleshooting.md)
- [Attachments and inline images](./docs/advanced/attachments.md)
- [Batch sending](./docs/advanced/batch-sending.md)
- [Resilience](./docs/advanced/resilience.md)
- [Testing](./docs/advanced/testing.md)
- [Full index](./docs/README.md)

## Versioning and support

MailVolt follows [Semantic Versioning](https://semver.org/). While the version is below
`1.0.0` the public API may still change between minor releases; breaking changes are called
out in [CHANGELOG.md](./CHANGELOG.md). All packages are versioned and released together.

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](./CONTRIBUTING.md),
[CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md) and [SECURITY.md](./SECURITY.md).

## License

[MIT](LICENSE)
