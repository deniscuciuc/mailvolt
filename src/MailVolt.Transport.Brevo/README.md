# MailVolt.Transport.Brevo

Brevo (formerly Sendinblue) transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.Brevo
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .AddBrevoSender(options =>
    {
        options.ApiKey = "your-api-key";
    });
```

## Documentation

- [Brevo Transport](../../docs/senders/brevo.md)
- [Getting Started](../../docs/getting-started.md)
