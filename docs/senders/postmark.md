# Postmark Transport

Sends emails via the [Postmark](https://postmarkapp.com/) API.

## Installation

```bash
dotnet add package MailVolt.Transport.Postmark
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UsePostmarkTransport(options =>
    {
        options.ApiKey = "your-server-token";
    });
```

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UsePostmarkTransport(
        builder.Configuration.GetSection("MailVolt:Postmark"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `ApiKey` | `string` | — | Yes |
| `MessageStream` | `string` | `"outbound"` | No |
| `BaseUrl` | `string` | `https://api.postmarkapp.com` | No |

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UsePostmarkTransport(options =>
    {
        options.ApiKey = Environment.GetEnvironmentVariable("POSTMARK_API_KEY")!;
        options.MessageStream = "transactional";
    });
```

```csharp
public class AlertService
{
    private readonly IEmailBuilder _builder;

    public AlertService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task<EmailResult> SendAlertAsync(string email, string alertMessage)
    {
        return await _builder
            .From("alerts@example.com")
            .To(email)
            .Subject("Critical Alert")
            .HtmlBody($"<p><strong>Alert:</strong> {alertMessage}</p>")
            .SendAsync();
    }
}
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "Postmark": {
      "ApiKey": "your-server-token",
      "MessageStream": "transactional"
    }
  }
}
```
