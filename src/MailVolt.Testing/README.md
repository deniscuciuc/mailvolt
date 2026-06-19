# MailVolt.Testing

Testing utilities for MailVolt — InMemorySender, assertions, and test helpers.

## Installation

```bash
dotnet add package MailVolt.Testing
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;
using MailVolt.Testing;

builder.Services.AddMailVolt()
    .UseInMemoryTransport();

var sender = services.GetRequiredService<InMemorySender>();

sender.Should().HaveCount(1);
sender.Should().ContainEmailTo("user@example.com");
```

## Documentation

- [Testing](../../docs/advanced/testing.md)
- [Getting Started](../../docs/getting-started.md)
