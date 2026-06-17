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

When `UseNativeTemplates` is enabled, reserve these `EmailMessage.Headers` keys for Mailgun template sends:

- `X-MailVolt-Template` → mapped to Mailgun's `template` form field
- `X-MailVolt-Template-Variables` → mapped to Mailgun's `t:variables` JSON field

Example:

```csharp
return await _builder
    .From("orders@example.com")
    .To(email)
    .Subject($"Order #{orderId} Confirmed")
    .Header("X-MailVolt-Template", "order-confirmation")
    .Header("X-MailVolt-Template-Variables", """{"orderId":42,"customer":"Alice"}""")
    .SendAsync();
```

When a Mailgun native template is selected this way, MailVolt omits `text` and `html` form fields so Mailgun renders the body from the provider-side template.

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
      "Domain": "mg.example.com",
      "UseNativeTemplates": true
    }
  }
}
```
