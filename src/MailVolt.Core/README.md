# MailVolt.Core

Core abstractions and fluent builder for MailVolt — modern .NET email library.

## Installation

```bash
dotnet add package MailVolt.Core
```

## Quick start

```csharp
using MailVolt.Core;

public class EmailService(IEmailBuilder builder)
{
    public async Task SendWelcomeAsync(string email)
    {
        var result = await builder
            .From("sender@example.com")
            .To(email)
            .Subject("Welcome to MailVolt!")
            .HtmlBody("<h1>Hello!</h1><p>Thanks for signing up.</p>")
            .SendAsync();

        if (result.IsSuccess)
            Console.WriteLine($"Sent! Message ID: {result.MessageId}");
    }
}
```

## Documentation

- [Getting Started](../../docs/getting-started.md)
