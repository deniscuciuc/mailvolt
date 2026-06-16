# Mailgun Transport

Sends emails via the [Mailgun](https://www.mailgun.com/) API v3.

## Installation

```bash
dotnet add package MailVolt.Transport.Mailgun
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseMailgunTransport(options =>
    {
        options.ApiKey = "your-api-key";
        options.Domain = "mg.example.com";
    });
```

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UseMailgunTransport(
        builder.Configuration.GetSection("MailVolt:Mailgun"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `ApiKey` | `string` | — | Yes |
| `Domain` | `string` | — | Yes |
| `BaseUrl` | `string` | `https://api.mailgun.net/v3` | No |
| `UseNativeTemplates` | `bool` | `false` | No |

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UseMailgunTransport(options =>
    {
        options.ApiKey = Environment.GetEnvironmentVariable("MAILGUN_API_KEY")!;
        options.Domain = "mg.example.com";
        options.UseNativeTemplates = true;
    });
```

```csharp
public class OrderService
{
    private readonly IEmailBuilder _builder;

    public OrderService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task<EmailResult> SendConfirmationAsync(string email, int orderId)
    {
        return await _builder
            .From("orders@example.com")
            .To(email)
            .Subject($"Order #{orderId} Confirmed")
            .HtmlBody($"<h1>Thank you!</h1><p>Order #{orderId} is confirmed.</p>")
            .SendAsync();
    }
}
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "Mailgun": {
      "ApiKey": "your-api-key",
      "Domain": "mg.example.com"
    }
  }
}
```
