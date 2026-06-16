# Brevo Transport

Sends emails via the [Brevo](https://www.brevo.com/) (formerly Sendinblue) API.

## Installation

```bash
dotnet add package MailVolt.Transport.Brevo
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseBrevoTransport(options =>
    {
        options.ApiKey = "your-api-key";
    });
```

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UseBrevoTransport(
        builder.Configuration.GetSection("MailVolt:Brevo"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `ApiKey` | `string` | — | Yes |

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UseBrevoTransport(options =>
    {
        options.ApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY")!;
    });
```

```csharp
public class CampaignService
{
    private readonly IEmailBuilder _builder;

    public CampaignService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task<EmailResult> SendNewsletterAsync(string email)
    {
        return await _builder
            .From("newsletter@example.com")
            .To(email)
            .Subject("Monthly Newsletter")
            .HtmlBody("<h1>Your monthly update</h1>")
            .SendAsync();
    }
}
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "Brevo": {
      "ApiKey": "your-api-key"
    }
  }
}
```
