# MailVolt.AutoConfigure

Zero-code configuration for MailVolt. Configure transport and templates entirely via appsettings.json — one line in Program.cs.

## Installation

```bash
dotnet add package MailVolt.AutoConfigure
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt(builder.Configuration);
```

Then configure the `MailVolt` section in `appsettings.json` to select the transport, credentials, and optional template engine.

## Documentation

- [AutoConfigure](../../docs/autoconfigure.md)
- [Getting Started](../../docs/getting-started.md)
