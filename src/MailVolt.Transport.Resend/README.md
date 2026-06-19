# MailVolt.Transport.Resend

Resend transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.Resend
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseResend(options =>
    {
        options.ApiKey = "re_your-api-key";
    });
```

## Documentation

- [Resend Transport](../../docs/senders/resend.md)
- [Getting Started](../../docs/getting-started.md)
