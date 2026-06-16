# Azure Email Transport

Sends emails via [Azure Communication Services Email](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview).

## Installation

```bash
dotnet add package MailVolt.Transport.AzureEmail
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseAzureEmailTransport(options =>
    {
        options.ConnectionString = "endpoint=https://...;accesskey=...";
    });
```

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UseAzureEmailTransport(
        builder.Configuration.GetSection("MailVolt:Azure"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `ConnectionString` | `string` | — | Yes |

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UseAzureEmailTransport(options =>
    {
        options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_EMAIL_CONNECTION")!;
    });
```

```csharp
public class NotificationService
{
    private readonly IEmailBuilder _builder;

    public NotificationService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task<EmailResult> SendAsync(string email)
    {
        return await _builder
            .From("noreply@example.com")
            .To(email)
            .Subject("Hello from Azure!")
            .HtmlBody("<h1>Sent via Azure Communication Services</h1>")
            .SendAsync();
    }
}
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "Azure": {
      "ConnectionString": "endpoint=https://...;accesskey=..."
    }
  }
}
```
