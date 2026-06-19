# MailVolt.Transport.Postmark

Postmark transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.Postmark
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .AddPostmarkSender(options =>
    {
        options.ApiKey = "your-server-token";
    });
```

## Documentation

- [Postmark Transport](../../docs/senders/postmark.md)
- [Getting Started](../../docs/getting-started.md)
