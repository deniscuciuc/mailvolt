# SendGrid Transport

Sends emails via the [SendGrid](https://sendgrid.com/) v3 API.

## Installation

```bash
dotnet add package MailVolt.Transport.SendGrid
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .AddSendGridSender(options =>
    {
        options.ApiKey = "SG.your-api-key";
    });
```

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .AddSendGridSender(
        builder.Configuration.GetSection("MailVolt:SendGrid"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `ApiKey` | `string` | — | Yes |
| `BaseUrl` | `string` | `https://api.sendgrid.com` | No |
| `UseDynamicTemplates` | `bool` | `false` | No |
| `SandboxMode` | `string?` | `null` | No |

## Full Example with Inline Attachments

SendGrid supports inline images embedded directly in HTML. Use `AsInlineImage()` on the attachment builder:

```csharp
builder.Services.AddMailVolt()
    .AddSendGridSender(options =>
    {
        options.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")!;
    });
```

```csharp
public async Task<EmailResult> SendReceiptAsync(string email)
{
    return await _builder
        .From("billing@example.com")
        .To(email)
        .Subject("Your Receipt")
        .HtmlBody("<h1>Receipt</h1><img src=\"cid:logo\" alt=\"Logo\" />")
        .Attach(attachment => attachment
            .FromFile("logo.png")
            .AsInlineImage("logo"))
        .SendAsync();
}
```

With dynamic templates:

```csharp
.AddSendGridSender(options =>
{
    options.ApiKey = "SG.your-api-key";
    options.UseDynamicTemplates = true;
})
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "SendGrid": {
      "ApiKey": "SG.your-api-key",
      "SandboxMode": true
    }
  }
}
```
