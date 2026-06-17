# Resend Transport

Sends emails via the [Resend](https://resend.com/) API.

## Installation

```bash
dotnet add package MailVolt.Transport.Resend
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseResend(options =>
    {
        options.ApiKey = "re_your-api-key";
    });
```

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UseResend(
        builder.Configuration.GetSection("MailVolt:Resend"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `ApiKey` | `string` | — | Yes |
| `BaseUrl` | `string` | `https://api.resend.com` | No |

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UseResend(options =>
    {
        options.ApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")!;
    });
```

```csharp
public class InviteService
{
    private readonly IEmailBuilder _builder;

    public InviteService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task<EmailResult> SendInviteAsync(string email, string inviteLink)
    {
        return await _builder
            .From("team@example.com")
            .To(email)
            .Subject("You're Invited!")
            .HtmlBody($"<p>Join us: <a href=\"{inviteLink}\">{inviteLink}</a></p>")
            .SendAsync();
    }
}
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "Resend": {
      "ApiKey": "re_your-api-key"
    }
  }
}
```
