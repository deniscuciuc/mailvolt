# MailVolt.AutoConfigure

Zero-code configuration for MailVolt. Configure transport and templates entirely
via `appsettings.json` — one line in `Program.cs`.

```csharp
// Program.cs
builder.Services.AddMailVolt(builder.Configuration);
```

That's it. The `MailVolt` section in `appsettings.json` drives everything:
transport selection, credentials, template engine, and default sender address.

---

## Configuration section

All settings live under the `"MailVolt"` section. At minimum you need a
`From` address and a `Transport` discriminator.

```json
{
  "MailVolt": {
    "From": {
      "Address": "noreply@example.com",
      "DisplayName": "My App"
    },
    "Transport": "Smtp",
    "Templates": "Razor"
  }
}
```

| Key | Required | Description |
|-----|----------|-------------|
| `From:Address` | **Yes** | Default sender email |
| `From:DisplayName` | No | Display name shown to recipients |
| `Transport` | **Yes** | One of: `Smtp`, `SendGrid`, `Mailgun`, `Resend`, `Postmark`, `Azure`, `Brevo`, `AwsSes`, `InMemory` |
| `Templates` | No | One of: `Razor`, `Liquid`, `Handlebars`. Omit to skip template engine |

---

## 1. SMTP

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com", "DisplayName": "MyApp" },
    "Transport": "Smtp",
    "Templates": "Razor",
    "Smtp": {
      "Host": "smtp.mailtrap.io",
      "Port": 587,
      "Username": "YOUR_USERNAME",
      "Password": "YOUR_PASSWORD",
      "Security": "StartTls",
      "TimeoutMs": 30000
    }
  }
}
```

`Security` values: `None` | `SslOnConnect` | `StartTls` | `StartTlsWhenAvailable`

---

## 2. SendGrid

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "SendGrid",
    "Templates": "Liquid",
    "SendGrid": {
      "ApiKey": "SG.YOUR_KEY_HERE",
      "BaseUrl": "https://api.sendgrid.com"
    }
  }
}
```

---

## 3. Mailgun

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@mg.example.com", "DisplayName": "MyApp" },
    "Transport": "Mailgun",
    "Mailgun": {
      "ApiKey": "YOUR_MAILGUN_KEY",
      "Domain": "mg.example.com",
      "BaseUrl": "https://api.mailgun.net/v3"
    }
  }
}
```

**EU region:** set `BaseUrl` to `"https://api.eu.mailgun.net/v3"`.

---

## 4. Resend

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "Resend",
    "Resend": {
      "ApiKey": "re_YOUR_KEY_HERE"
    }
  }
}
```

---

## 5. Postmark

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "Postmark",
    "Postmark": {
      "ApiKey": "YOUR_POSTMARK_SERVER_TOKEN",
      "MessageStream": "outbound"
    }
  }
}
```

---

## 6. Azure Communication Services

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "Azure",
    "Azure": {
      "ConnectionString": "endpoint=https://your-service.communication.azure.com/;accesskey=YOUR_KEY"
    }
  }
}
```

---

## 7. Brevo (Sendinblue)

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "Brevo",
    "Brevo": {
      "ApiKey": "YOUR_BREVO_API_KEY"
    }
  }
}
```

---

## 8. AWS SES

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "AwsSes",
    "AwsSes": {
      "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
      "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
      "Region": "us-east-1",
      "ConfigurationSetName": "my-config-set"
    }
  }
}
```

---

## 9. InMemory (testing / development)

No transport sub-section needed. The `InMemorySender` captures all sent emails
for inspection — no real emails are dispatched.

```json
{
  "MailVolt": {
    "From": { "Address": "noreply@example.com" },
    "Transport": "InMemory"
  }
}
```

---

## Environment variable overrides

Any setting in the `MailVolt` section can be overridden via environment
variables using the double-underscore (`__`) separator:

| Environment variable | Overrides |
|----------------------|-----------|
| `MailVolt__Transport` | `MailVolt:Transport` |
| `MailVolt__From__Address` | `MailVolt:From:Address` |
| `MailVolt__From__DisplayName` | `MailVolt:From:DisplayName` |
| `MailVolt__Smtp__Host` | `MailVolt:Smtp:Host` |
| `MailVolt__Smtp__Port` | `MailVolt:Smtp:Port` |
| `MailVolt__SendGrid__ApiKey` | `MailVolt:SendGrid:ApiKey` |
| `MailVolt__Mailgun__ApiKey` | `MailVolt:Mailgun:ApiKey` |
| `MailVolt__Mailgun__Domain` | `MailVolt:Mailgun:Domain` |
| `MailVolt__Resend__ApiKey` | `MailVolt:Resend:ApiKey` |
| `MailVolt__Postmark__ApiKey` | `MailVolt:Postmark:ApiKey` |
| `MailVolt__Azure__ConnectionString` | `MailVolt:Azure:ConnectionString` |
| `MailVolt__Brevo__ApiKey` | `MailVolt:Brevo:ApiKey` |
| `MailVolt__AwsSes__AccessKeyId` | `MailVolt:AwsSes:AccessKeyId` |
| `MailVolt__AwsSes__SecretAccessKey` | `MailVolt:AwsSes:SecretAccessKey` |
| `MailVolt__AwsSes__Region` | `MailVolt:AwsSes:Region` |

Example — switch transport via environment:

```bash
export MailVolt__Transport=SendGrid
export MailVolt__SendGrid__ApiKey=SG.xxxxx
```

---

## Docker Compose example

```yaml
services:
  app:
    image: my-app:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MailVolt__Transport=Smtp
      - MailVolt__From__Address=noreply@example.com
      - MailVolt__Smtp__Host=smtp.sendgrid.net
      - MailVolt__Smtp__Port=587
      - MailVolt__Smtp__Username=apikey
      - MailVolt__Smtp__Password=SG.xxxxx
      - MailVolt__Smtp__Security=StartTls
```

---

## Usage in code

```csharp
// Minimal — one line
builder.Services.AddMailVolt(builder.Configuration);

// Custom section name
builder.Services.AddMailVolt(builder.Configuration, "MyEmailConfig");
```

No need to call `UseSmtpTransport`, `UseRazorTemplates`, etc. — the extension
method reads the configuration and wires everything automatically.
