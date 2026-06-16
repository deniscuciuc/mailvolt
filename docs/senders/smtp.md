# SMTP Transport

Sends emails via SMTP using [MailKit](http://www.mimekit.net/docs).

## Installation

```bash
dotnet add package MailVolt.Transport.Smtp
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options =>
    {
        options.Host = "smtp.example.com";
        options.Username = "user@example.com";
        options.Password = "your-password";
    });
```

You can also bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UseSmtpTransport(
        builder.Configuration.GetSection("MailVolt:Smtp"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `Host` | `string` | — | Yes |
| `Port` | `int` | `587` | No |
| `Username` | `string?` | `null` | No |
| `Password` | `string?` | `null` | No |
| `Security` | `SecureSocketOptions` | `StartTlsWhenAvailable` | No |
| `TimeoutMs` | `int` | `30000` | No |
| `OAuth2TokenProvider` | `Func<CancellationToken, Task<string>>?` | `null` | No |

### Security values

`SecureSocketOptions` (from MailKit):

| Value | Description |
|---|---|
| `None` | No SSL/TLS |
| `Auto` | Negotiate automatically |
| `SslOnConnect` | SMTPS on connect |
| `StartTlsWhenAvailable` | Upgrade if server supports it (default) |
| `StartTls` | Require TLS upgrade |

### OAuth2

Provide a token provider for OAuth2 authentication:

```csharp
.UseSmtpTransport(options =>
{
    options.Host = "smtp.gmail.com";
    options.Port = 587;
    options.OAuth2TokenProvider = async ct =>
    {
        return await GetAccessTokenAsync();
    };
})
```

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UseSmtpTransport(options =>
    {
        options.Host = "smtp.example.com";
        options.Port = 587;
        options.Username = "user@example.com";
        options.Password = "secret";
        options.Security = SecureSocketOptions.StartTls;
        options.TimeoutMs = 15000;
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

    public async Task<EmailResult> NotifyAsync(string email, string message)
    {
        return await _builder
            .From("noreply@example.com")
            .To(email)
            .Subject("New Notification")
            .HtmlBody($"<p>{message}</p>")
            .SendAsync();
    }
}
```

## Additional Resources

- [MailKit Documentation](http://www.mimekit.net/docs)
