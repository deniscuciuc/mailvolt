# Getting Started with MailVolt

MailVolt is a modern, async-first, DI-first .NET email library. This guide walks you from installation to sending your first email.

## Prerequisites

- .NET 8.0 SDK or later
- An ASP.NET Core project (or any `IServiceCollection`-hosted application)

## 1. Installation

Install the core package and at least one transport:

```bash
dotnet add package MailVolt.Core
dotnet add package MailVolt.Transport.Smtp
```

For a full list of available packages, see the [README](../README.md).

## 2. Basic SMTP Setup

Register MailVolt with SMTP transport in your `Program.cs`:

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

Then send your first email:

```csharp
public class EmailService
{
    private readonly IEmailBuilder _builder;

    public EmailService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task SendWelcomeEmailAsync()
    {
        var result = await _builder
            .From("sender@example.com")
            .To("recipient@example.com")
            .Subject("Welcome to MailVolt!")
            .HtmlBody("<h1>Hello!</h1><p>Thanks for signing up.</p>")
            .SendAsync();

        if (result.IsSuccess)
        {
            Console.WriteLine($"Sent! Message ID: {result.MessageId}");
        }
    }
}
```

## 3. SendGrid Setup

```csharp
dotnet add package MailVolt.Transport.SendGrid
```

```csharp
builder.Services.AddMailVolt()
    .AddSendGridSender(options =>
    {
        options.ApiKey = "SG.your-api-key";
    });
```

## 4. Using Razor Templates

```csharp
dotnet add package MailVolt.Templates.Razor
```

```csharp
builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseRazorTemplates();
```

Create a template file `Emails/Welcome.cshtml`:

```html
@model WelcomeModel

<h1>Welcome, @Model.Name!</h1>
<p>Click <a href="@Model.ConfirmUrl">here</a> to confirm your account.</p>
```

Send with the template:

```csharp
var result = await _builder
    .From("sender@example.com")
    .To("user@example.com")
    .Subject("Welcome!")
    .UsingTemplate("Emails/Welcome.cshtml", new WelcomeModel
    {
        Name = "Alice",
        ConfirmUrl = "https://example.com/confirm?token=abc"
    })
    .SendAsync();
```

## 5. Testing with InMemorySender

For unit tests, use the in-memory transport — no real emails sent:

```csharp
dotnet add package MailVolt.Testing
```

```csharp
builder.Services.AddMailVolt()
    .UseInMemoryTransport();
```

Assert against sent emails:

```csharp
var sender = services.GetRequiredService<InMemorySender>();

sender.Should().HaveCount(1);
sender.Should().ContainEmailTo("user@example.com");
sender.Should().ContainSubject("Welcome!");
```

## 6. Configuration from `appsettings.json`

Create a configuration section and bind it:

```json
{
  "MailVolt": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "user@example.com",
      "Password": "your-password"
    }
  }
}
```

```csharp
builder.Services.AddMailVolt()
    .UseSmtpTransport(
        builder.Configuration.GetSection("MailVolt:Smtp"));
```

## Next Steps

- Explore all [senders](./senders/smtp.md) (SMTP, SendGrid, Mailgun, and more)
- Learn about [templates](./templates/razor.md) (Razor, Liquid, Handlebars)
- Read about [batch sending](./advanced/batch-sending.md) and [resilience](./advanced/resilience.md)
