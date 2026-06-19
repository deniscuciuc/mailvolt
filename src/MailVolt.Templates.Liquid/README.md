# MailVolt.Templates.Liquid

Liquid template rendering for MailVolt using Fluid.Core.

## Installation

```bash
dotnet add package MailVolt.Templates.Liquid
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseLiquidTemplates();

var result = await builder
    .From("noreply@example.com")
    .To("user@example.com")
    .Subject("Your Invoice")
    .UsingTemplate("Emails/invoice.liquid", new { CustomerName = "Alice", Total = 49.99m })
    .SendAsync();
```

## Documentation

- [Liquid Templates](../../docs/templates/liquid.md)
- [Getting Started](../../docs/getting-started.md)
