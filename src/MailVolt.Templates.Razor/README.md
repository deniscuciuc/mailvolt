# MailVolt.Templates.Razor

Razor template rendering for MailVolt using native ASP.NET Core Razor.

## Installation

```bash
dotnet add package MailVolt.Templates.Razor
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseRazorTemplates();

var result = await builder
    .From("noreply@example.com")
    .To("user@example.com")
    .Subject("Welcome!")
    .UsingTemplate("Emails/Welcome.cshtml", new { Name = "Alice" })
    .SendAsync();
```

## Documentation

- [Razor Templates](../../docs/templates/razor.md)
- [Getting Started](../../docs/getting-started.md)
