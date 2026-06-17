# MailVolt Examples

Quick-start examples showing different combinations of transports and templates.
Each example is fully standalone — just add your credentials and `dotnet run`.

| # | Example | Transport | Template | Pattern |
|---|---------|-----------|----------|---------|
| 01 | [Console · SMTP + Razor](./01-console-smtp-razor/) | SMTP | Razor | Welcome email |
| 02 | [Console · SendGrid + Liquid](./02-console-sendgrid-liquid/) | SendGrid | Liquid | Order confirmation |
| 03 | [Console · Mailgun + Handlebars](./03-console-mailgun-handlebars/) | Mailgun | Handlebars | Password reset |
| 04 | [Console · Resend + Batch](./04-console-resend-batch/) | Resend | Liquid | Newsletter bulk send |
| 05 | [Minimal API · SMTP](./05-minimalapi-smtp/) | SMTP | Inline HTML | Contact form endpoint |
| 06 | [Worker · Scheduled Digest](./06-worker-scheduler/) | SMTP | Liquid | Background daily digest |
| 07 | [Console · Inline Images](./07-inline-images/) | SMTP | Razor | Invoice with CID logo |
| 08 | [Console · Multi-Provider Fallback](./08-multi-provider-fallback/) | Resend → SMTP | Inline HTML | Resilience pattern |
| 09 | [Testing Demo](./09-testing-demo/) | InMemory | None | Unit testing with InMemorySender |

## Quick start (any example)

```bash
# 1. Copy config
cp appsettings.example.json appsettings.json

# 2. Fill in your credentials (see provider guides below)

# 3. Run
dotnet run                  # sends real email
dotnet run -- --dry-run     # InMemorySender — no email sent, prints result
```

## Local SMTP testing with Mailpit

The SMTP-based examples (01, 05, 06, 07, 08) can be tested locally without a real mail server using [Mailpit](https://github.com/axllent/mailpit).

Start Mailpit from this directory:

```bash
cd examples
docker compose up -d
```

This exposes:

- SMTP server on `localhost:1025` (no authentication required)
- Web UI on http://localhost:8025

The `appsettings.example.json` files for SMTP examples are already configured for Mailpit:

```json
"Smtp": {
  "Host": "localhost",
  "Port": 1025,
  "Security": "None"
}
```

Run an SMTP example against Mailpit:

```bash
cd examples/01-console-smtp-razor
cp appsettings.example.json appsettings.json
dotnet run
```

Open http://localhost:8025 to inspect the captured messages.

To stop Mailpit:

```bash
cd examples
docker compose down
```

## Environment variables

Every example can also be configured via environment variables using the standard .NET `__` separator. There is no custom prefix, so a setting in `MailVolt:Smtp:Host` becomes:

```bash
export MailVolt__Smtp__Host=localhost
export MailVolt__Smtp__Port=1025
export MailVolt__Smtp__Security=None
export MailVolt__DefaultFromAddress=noreply@example.com
```

For cloud providers, set the API key environment variables shown in the table below.

## Provider setup guide

Most examples need an API key from the matching provider. Sign up for the free tier, generate a key, and copy it into `appsettings.json` (or set the matching environment variable).

| Provider | Example(s) | How to get a key | Config key / env var |
|----------|------------|------------------|----------------------|
| **SMTP / Mailpit** | 01, 05, 06, 07, 08 | Run `docker compose up -d` in `examples/` | `MailVolt:Smtp:Host` / `MailVolt__Smtp__Host` |
| **SendGrid** | 02 | [SendGrid API Keys](https://app.sendgrid.com/settings/api_keys) | `MailVolt:SendGrid:ApiKey` / `MailVolt__SendGrid__ApiKey` |
| **Mailgun** | 03 | [Mailgun dashboard](https://app.mailgun.com/app/account/security/api_keys) | `MailVolt:Mailgun:ApiKey`, `MailVolt:Mailgun:Domain` |
| **Resend** | 04, 08 | [Resend API Keys](https://resend.com/api-keys) | `MailVolt:Resend:ApiKey` / `MailVolt__Resend__ApiKey` |
| **Postmark** | — | [Postmark server API token](https://account.postmarkapp.com/servers/) | `MailVolt:Postmark:ApiKey` |
| **Azure Communication Email** | — | [Azure Communication Services](https://portal.azure.com/) | `MailVolt:Azure:ConnectionString` |
| **Brevo** | — | [Brevo API keys](https://app.brevo.com/settings/keys/api) | `MailVolt:Brevo:ApiKey` |
| **AWS SES** | — | [AWS IAM credentials](https://aws.amazon.com/iam/) | `MailVolt:AwsSes:AccessKeyId`, `MailVolt:AwsSes:SecretAccessKey`, `MailVolt:AwsSes:Region` |

> **Security:** never commit API keys. `appsettings.json` is gitignored in every example. Use `appsettings.example.json` as a template only.

## Recommended order for learning

1. Start with **01** (simplest)
2. **09** to understand testing
3. **04** for batch sending
4. **08** for production resilience pattern
