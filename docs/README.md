# MailVolt documentation

## Start here

- **[Getting started](./getting-started.md)** — install, register a transport, send your first email.
- **[Configuration reference](./autoconfigure.md)** — every key under the `MailVolt` section, for all nine transports and three template engines.
- **[Migrating from FluentEmail](./migrating-from-fluentemail.md)** — an API-by-API mapping.
- **[Troubleshooting](./troubleshooting.md)** — why no email was sent and no error was reported.

## Transports

| Provider | Registration | Page |
|---|---|---|
| SMTP (MailKit) | `UseSmtpTransport` | [smtp.md](./senders/smtp.md) |
| SendGrid | `UseSendGridTransport` | [sendgrid.md](./senders/sendgrid.md) |
| Mailgun | `UseMailgunTransport` | [mailgun.md](./senders/mailgun.md) |
| Resend | `UseResendTransport` | [resend.md](./senders/resend.md) |
| Postmark | `UsePostmarkTransport` | [postmark.md](./senders/postmark.md) |
| Azure Email | `UseAzureEmailTransport` | [azure.md](./senders/azure.md) |
| Brevo | `UseBrevoTransport` | [brevo.md](./senders/brevo.md) |
| AWS SES | `UseAwsSesTransport` | [aws-ses.md](./senders/aws-ses.md) |

Every transport's `IConfiguration` overload takes the configuration **root** and resolves its
own `MailVolt:<Provider>` section. Passing a pre-scoped section instead resolves to
`MailVolt:<Provider>:MailVolt:<Provider>` and silently produces empty options.

## Templates

- [Razor](./templates/razor.md) — `.cshtml` views, layouts, strongly typed models.
- [Liquid](./templates/liquid.md) — Fluid, safe for user-authored templates.
- [Handlebars](./templates/handlebars.md) — helpers and partials.

## Advanced

- [Attachments and inline images](./advanced/attachments.md)
- [Batch sending](./advanced/batch-sending.md)
- [Resilience](./advanced/resilience.md)
- [Testing](./advanced/testing.md)

## Maintainers

- [Release process](./release.md)
