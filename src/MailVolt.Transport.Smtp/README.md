# MailVolt.Transport.Smtp

SMTP transport for MailVolt using MailKit.

## Installation

```bash
dotnet add package MailVolt.Transport.Smtp
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options =>
    {
        options.Host = "smtp.example.com";
        options.Port = 587;
        options.Username = "user@example.com";
        options.Password = "your-password";
    });
```

## Documentation

- [SMTP Transport](../../docs/senders/smtp.md)
- [Getting Started](../../docs/getting-started.md)
