# MailVolt.Transport.SendGrid

SendGrid transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.SendGrid
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .AddSendGridSender(options =>
    {
        options.ApiKey = "SG.your-api-key";
    });
```

## Documentation

- [SendGrid Transport](../../docs/senders/sendgrid.md)
- [Getting Started](../../docs/getting-started.md)
