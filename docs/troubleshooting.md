# Troubleshooting

Every failure mode below is one MailVolt used to hit silently. They are listed with the
symptom first, because that is what you have when you go looking.

## No email is sent and no error is reported

### The configuration section did not bind

**Symptom.** `SendAsync` returns a failure mentioning a missing host or API key, or the
send goes to `localhost` instead of your provider.

**Cause.** Every transport's `IConfiguration` overload takes the configuration **root** and
resolves its own section:

```csharp
// Correct — MailVolt reads MailVolt:Smtp itself.
builder.Services.AddMailVolt().UseSmtpTransport(builder.Configuration);

// Wrong — resolves to MailVolt:Smtp:MailVolt:Smtp, so every option stays at its default.
builder.Services.AddMailVolt()
    .UseSmtpTransport(builder.Configuration.GetSection("MailVolt:Smtp"));
```

**Check.** Resolve the options and inspect them:

```csharp
var options = provider.GetRequiredService<IOptions<SmtpSenderOptions>>().Value;
Console.WriteLine(options.Host); // null means the section did not bind
```

### You called the wrong `AddMailVolt`

**Symptom.** `IEmailBuilder` resolves, but `SendAsync` throws
`An ISender must be registered in the DI container`.

**Cause.** `MailVolt.Core` and `MailVolt.AutoConfigure` both provide `AddMailVolt`.
Core's overload registers the builder and batch sender but **no transport** — it is the
starting point for explicit registration. AutoConfigure's overload wires a transport from
configuration.

```csharp
// AutoConfigure: reads MailVolt:Transport and registers that transport.
builder.Services.AddMailVolt(builder.Configuration);

// Core: you choose the transport yourself.
builder.Services.AddMailVolt().UseSmtpTransport(builder.Configuration);
```

If both packages are referenced, the AutoConfigure form is the one that takes
`IConfiguration` and returns `IServiceCollection`; Core's returns `MailVoltBuilder`.

## The email arrives with an empty body

You called `UsingTemplate` but no `ITemplateRenderer` is registered, or the model was
`null`. Both now throw with a message naming the missing registration, rather than sending
a body-less email:

```csharp
builder.Services.AddMailVolt()
    .UseSmtpTransport(builder.Configuration)
    .UseRazorTemplates();   // ← this is what was missing
```

## An attachment arrives empty on a retry

Fixed. `EmailAttachment.Content` is `ReadOnlyMemory<byte>`, so an `EmailMessage` can be sent
any number of times. If you are on `0.1.0-preview.3` or earlier, `Content` was a `Stream`
that the first send consumed, so a retry — including Mailgun's automatic retry — sent an
empty attachment. Upgrade.

## Recipients from one email appear on another

Fixed. Do not reuse an `IEmailBuilder` across messages regardless: it is registered
transient precisely so each send gets a fresh one. If you inject it into a singleton, resolve
a new builder per send instead:

```csharp
public sealed class Notifier(IServiceProvider services)
{
    public Task SendAsync(string to) =>
        services.GetRequiredService<IEmailBuilder>()
            .To(to)
            .Subject("Hello")
            .TextBody("Hi")
            .SendAsync();
}
```

On `0.1.0-preview.3` and earlier, building two messages from one builder aliased the
recipient list, so the first message's recipients changed after the fact.

## A cancelled send behaves differently per provider

Fixed. Every transport now lets `OperationCanceledException` propagate and reserves
`EmailResult.Failure` for real send failures. Earlier versions were inconsistent: SMTP, AWS
SES and Azure turned a cancellation into a failed result, and Postmark returned a failure
whose message said "cancelled".

## An inline image shows up as a download instead of rendering

The `cid:` reference in your HTML must match the `AsInlineImage` content ID exactly:

```csharp
.HtmlBody("""<img src="cid:logo@mailvolt" />""")
.Attach(a => a.FromFile("Assets/logo.png").AsInlineImage("logo@mailvolt"))
```

Note the ordering: call `AsInlineImage` **after** `FromFile`, and if the file extension does
not identify the type, set it explicitly with `WithContentType`.

Earlier versions dropped inline handling entirely on AWS SES and Azure, and `AsInlineImage`
overwrote a correctly detected content type with `image/png`.

## Razor cannot find the view

`RazorTemplateOptions.RootDirectory` defaults to the process working directory, which is not
the output directory when you run from an IDE. Pass the template name as the view name and
make sure the `.cshtml` is copied to the output:

```xml
<ItemGroup>
  <Content Update="Templates/**/*.cshtml" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

`HandlebarsTemplateRenderer` resolves a relative path against the working directory first,
then `AppContext.BaseDirectory`, and finally treats the string itself as the template source.

## Batch counts do not add up

They do now: `SentCount + FailedCount + SkippedCount == TotalCount`. `SkippedCount` covers
emails the batch never attempted, because `FailureStrategy.StopOnFirstFailure` halted it or
the caller cancelled. `Results` is ordered by completion, not by input order, and contains an
entry only for the emails that were attempted.

## Building from source fails with a syntax error

You need the **.NET 10 SDK**. `global.json` pins it, so the error should name the version —
if it does not, check `dotnet --version` reports 10.0.100 or later. Consuming the published
packages only needs a net8.0 or later runtime.

## Still stuck

Open an issue with the transport, the MailVolt version, and the `EmailResult.Error` text:
<https://github.com/deniscuciuc/mailvolt/issues>.
