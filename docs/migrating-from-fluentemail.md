# Migrating from FluentEmail

MailVolt covers the same ground as FluentEmail with a DI-first, async-only API. This page maps
each FluentEmail concept onto its MailVolt equivalent.

## Packages

| FluentEmail | MailVolt |
|---|---|
| `FluentEmail.Core` | `MailVolt.Core` |
| `FluentEmail.Smtp` | `MailVolt.Transport.Smtp` |
| `FluentEmail.SendGrid` | `MailVolt.Transport.SendGrid` |
| `FluentEmail.Mailgun` | `MailVolt.Transport.Mailgun` |
| `FluentEmail.MailKit` | `MailVolt.Transport.Smtp` (MailKit is the implementation) |
| `FluentEmail.Razor` | `MailVolt.Templates.Razor` |
| `FluentEmail.Liquid` | `MailVolt.Templates.Liquid` |
| `FluentEmail.Handlebars` | `MailVolt.Templates.Handlebars` |
| — | `MailVolt.Transport.{Resend,Postmark,AzureEmail,Brevo,AwsSes}` |
| — | `MailVolt.AutoConfigure`, `MailVolt.Testing` |

## Registration

FluentEmail:

```csharp
services
    .AddFluentEmail("noreply@example.com", "My App")
    .AddRazorRenderer()
    .AddSmtpSender("smtp.example.com", 587);
```

MailVolt:

```csharp
using MailVolt.Core.DependencyInjection;

services
    .AddMailVolt(options =>
    {
        options.DefaultFromAddress = "noreply@example.com";
        options.DefaultFromDisplayName = "My App";
    })
    .UseRazorTemplates()
    .UseSmtpTransport(options =>
    {
        options.Host = "smtp.example.com";
        options.Port = 587;
    });
```

One `using` covers `AddMailVolt` and every transport and template engine.

Or skip registration code entirely with `MailVolt.AutoConfigure` — see
[the configuration reference](./autoconfigure.md).

## Sending

FluentEmail's `IFluentEmail` becomes MailVolt's `IEmailBuilder`. The fluent chain is nearly
identical; the differences are that MailVolt has no synchronous send and takes a
`CancellationToken`.

```csharp
// FluentEmail
var response = await _email
    .To("user@example.com")
    .Subject("Welcome")
    .Body("<h1>Hi</h1>", isHtml: true)
    .SendAsync();

if (!response.Successful) { /* response.ErrorMessages */ }
```

```csharp
// MailVolt
var result = await _email
    .To("user@example.com")
    .Subject("Welcome")
    .HtmlBody("<h1>Hi</h1>")
    .SendAsync(cancellationToken);

if (result.IsFailure) { /* result.Error, result.Exception */ }
```

### Method mapping

| FluentEmail | MailVolt | Notes |
|---|---|---|
| `IFluentEmail` | `IEmailBuilder` | Both transient; resolve one per message |
| `.To(email, name)` | `.To(new EmailAddress(email, name))` | A bare string also works via implicit conversion |
| `.CC(...)` / `.BCC(...)` | `.Cc(...)` / `.Bcc(...)` | Casing differs |
| `.Body(html, isHtml: true)` | `.HtmlBody(html)` | Separate `.TextBody(...)` for plain text |
| `.Body(text)` | `.TextBody(text)` | `.Body(...)` is an alias for `.TextBody(...)` |
| `.HighPriority()` / `.LowPriority()` | `.Priority(EmailPriority.High)` / `.Priority(EmailPriority.Low)` | |
| `.Tag(...)` | `.Tag(...)` | |
| `.Header(k, v)` | `.Header(k, v)` | |
| `.Attach(new Attachment { ... })` | `.Attach(a => a.FromFile(path))` | See [attachments](./advanced/attachments.md) |
| `.UsingTemplate(template, model)` | `.UsingTemplate(template, model)` | |
| `.UsingTemplateFromFile(path, model)` | `.UsingTemplate(path, model)` | The renderer resolves paths |
| `.SendAsync()` | `.SendAsync(cancellationToken)` | |
| `.Send()` | — | No synchronous send; MailVolt is async-only |
| `SendResponse.Successful` | `EmailResult.IsSuccess` | Also `IsFailure` |
| `SendResponse.ErrorMessages` | `EmailResult.Error` | A single string, plus `EmailResult.Exception` |
| `SendResponse.MessageId` | `EmailResult.MessageId` | |
| `Email.From(...)` (static) | — | No static entry point; resolve `IEmailBuilder` from DI |

### There is no static entry point

FluentEmail's `Email.DefaultSender` / `Email.From(...)` static API has no MailVolt equivalent.
Inject `IEmailBuilder` instead. If you are migrating code that calls the static API from
somewhere without DI, resolve it from the service provider at the call site:

```csharp
var email = serviceProvider.GetRequiredService<IEmailBuilder>();
```

## Templates

Razor templates carry over as-is. MailVolt uses ASP.NET Core's own Razor view engine rather
than RazorLight, so `@model`, layouts and partials behave the way they do in MVC:

```csharp
services.AddMailVolt().UseRazorTemplates(options => options.RootDirectory = "Templates");
```

```csharp
await _email
    .To("user@example.com")
    .Subject("Welcome")
    .UsingTemplate("Welcome", new WelcomeModel { Name = "Ada" })
    .SendAsync(cancellationToken);
```

A template requires a registered renderer **and** a non-null model. MailVolt throws if either
is missing, rather than sending an email with an empty body.

## Batch sending

FluentEmail has no batch API — you loop and send. MailVolt has `IBatchEmailSender` with
concurrency control, rate limiting and a failure strategy:

```csharp
var messages = new List<EmailMessage>();
foreach (var recipient in recipients)
{
    messages.Add(await builder.To(recipient).Subject("Digest").TextBody(body)
        .BuildAsync(cancellationToken));
}

var result = await batchSender.SendBatchAsync(messages, new BatchSendOptions(
    MaxConcurrency: 10,
    DelayMs: 200,
    FailureStrategy: FailureStrategy.Continue), cancellationToken);
```

See [batch sending](./advanced/batch-sending.md).

## Testing

FluentEmail is usually tested by mocking `IFluentEmail` or asserting on a fake sender.
MailVolt ships an in-memory transport and assertion helpers:

```csharp
services.AddMailVolt().UseInMemoryTransport();
var sender = provider.GetRequiredService<InMemorySender>();

await subject.DoWorkAsync();

sender.Should().HaveCount(1).ContainEmailTo("user@example.com");
```

See [testing](./advanced/testing.md).

## Behaviour differences worth knowing

- **Async only.** There is no `Send()`. If you need to call from synchronous code, that is a
  decision to make at your call site, not one MailVolt makes for you.
- **Cancellation throws.** A cancelled send raises `OperationCanceledException` on every
  transport, rather than returning a failed result.
- **Attachments are bytes, not streams.** `EmailAttachment.Content` is
  `ReadOnlyMemory<byte>`, so a message is immutable and can be sent more than once.
- **Address validation.** `EmailAddress` rejects malformed addresses at construction rather
  than surfacing an opaque provider error later. Use `EmailAddress.TryParse` if you would
  rather not catch.
- **A missing `From` is an error.** MailVolt throws unless `From` is set on the message or
  `MailVoltOptions.DefaultFromAddress` is configured.
