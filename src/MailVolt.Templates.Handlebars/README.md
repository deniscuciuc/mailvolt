# MailVolt.Templates.Handlebars

Handlebars template rendering for MailVolt using Handlebars.Net.

## Installation

```bash
dotnet add package MailVolt.Templates.Handlebars
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseHandlebarsTemplates();

var result = await builder
    .From("noreply@example.com")
    .To("user@example.com")
    .Subject("Your Receipt")
    .UsingTemplate("Emails/receipt.hbs", new { CustomerName = "Alice" })
    .SendAsync();
```

## Documentation

- [Handlebars Templates](../../docs/templates/handlebars.md)
- [Getting Started](../../docs/getting-started.md)
