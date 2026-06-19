# MailVolt.Transport.Mailgun

Mailgun transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.Mailgun
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseMailgunTransport(options =>
    {
        options.ApiKey = "your-api-key";
        options.Domain = "mg.example.com";
    });
```

## Documentation

- [Mailgun Transport](../../docs/senders/mailgun.md)
- [Getting Started](../../docs/getting-started.md)
