# MailVolt.Transport.AzureEmail

Azure Email Communication Services transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.AzureEmail
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseAzureEmailTransport(options =>
    {
        options.ConnectionString = "endpoint=https://...;accesskey=...";
    });
```

## Documentation

- [Azure Email Transport](../../docs/senders/azure.md)
- [Getting Started](../../docs/getting-started.md)
