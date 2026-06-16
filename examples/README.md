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

# 2. Fill in your credentials (use Mailtrap for safe testing)
# https://mailtrap.io/register/signup

# 3. Run
dotnet run                  # sends real email
dotnet run -- --dry-run     # InMemorySender — no email sent, prints result
```

## Recommended order for learning

1. Start with **01** (simplest)
2. **09** to understand testing
3. **04** for batch sending
4. **08** for production resilience pattern
