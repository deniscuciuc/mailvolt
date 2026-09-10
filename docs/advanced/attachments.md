# Attachments and inline images

Attachments are described with `Attach`, which takes a callback on `IAttachmentBuilder`:

```csharp
await email
    .To("user@example.com")
    .Subject("Your invoice")
    .TextBody("The invoice is attached.")
    .Attach(a => a.FromFile("invoices/2026-09.pdf"))
    .SendAsync(cancellationToken);
```

## Sources

| Method | Use when |
|---|---|
| `FromFile(path)` | The content is a file on disk. The file name and content type are inferred from the path. |
| `FromBytes(fileName, bytes)` | You already have a `byte[]`. |
| `FromMemory(fileName, content)` | You have a `ReadOnlyMemory<byte>` and want to avoid a copy. |
| `FromStream(fileName, stream)` | The content only exists as a stream. It is read into memory when the message is built; the caller keeps ownership of the stream and is responsible for disposing it. |

The file read is deferred to `BuildAsync`, so `FromFile` does no I/O at the point you call it
and the read happens asynchronously.

## Content types

The content type is inferred from the file extension, with `application/octet-stream` as the
fallback. Override it explicitly when the extension is missing or misleading:

```csharp
.Attach(a => a
    .FromBytes("export", csvBytes)
    .WithFileName("export.csv")
    .WithContentType("text/csv"))
```

## Inline images

An inline image is an attachment with a content ID, referenced from the HTML body with
`cid:`:

```csharp
await email
    .To("user@example.com")
    .Subject("Welcome")
    .HtmlBody("""
        <h1>Welcome</h1>
        <img src="cid:logo@mailvolt" alt="Our logo" />
        """)
    .Attach(a => a.FromFile("Assets/logo.png").AsInlineImage("logo@mailvolt"))
    .SendAsync(cancellationToken);
```

The `cid:` value must match the `AsInlineImage` argument exactly. Call `AsInlineImage` after
the source method, so the content type is already detected — otherwise `AsInlineImage` falls
back to `image/png`, which is only right for a PNG. When the extension does not identify the
type, set it explicitly:

```csharp
.Attach(a => a
    .FromBytes("logo", bytes)
    .WithContentType("image/svg+xml")
    .AsInlineImage("logo@mailvolt"))
```

`EmailAttachment.IsInline` is `true` whenever `ContentId` is set. Each transport maps that to
its provider's inline mechanism — a MIME linked resource for SMTP and AWS SES, a
`Content-ID` part for Mailgun, `ContentId` for Azure, and the provider's inline disposition
for SendGrid, Postmark and Resend.

## Multiple attachments

Call `Attach` once per attachment. Inline images and ordinary attachments can be mixed:

```csharp
await email
    .To("user@example.com")
    .Subject("Monthly report")
    .HtmlBody("""<img src="cid:logo@mailvolt" /><p>Report attached.</p>""")
    .Attach(a => a.FromFile("Assets/logo.png").AsInlineImage("logo@mailvolt"))
    .Attach(a => a.FromFile("reports/2026-09.pdf"))
    .SendAsync(cancellationToken);
```

## Reading an attachment back

`EmailAttachment.Content` is a `ReadOnlyMemory<byte>`, so it can be read any number of times.
`OpenReadStream()` returns a fresh read-only stream over it for APIs that need one:

```csharp
var message = await email.To("user@example.com").Subject("x").TextBody("y")
    .Attach(a => a.FromFile("report.pdf"))
    .BuildAsync(cancellationToken);

using var stream = message.Attachments[0].OpenReadStream();
```

Because the content is bytes rather than a live stream, the same `EmailMessage` can be sent
more than once — which matters when a transport's resilience handler retries a request.

## Provider limits

Providers cap total message size, and the cap counts base64 encoding overhead — roughly a
third more than the raw bytes. Typical limits at time of writing:

| Provider | Approximate limit |
|---|---|
| SendGrid | 30 MB |
| Mailgun | 25 MB |
| Postmark | 10 MB |
| Resend | 40 MB |
| Azure Email | 10 MB |
| Brevo | 10 MB |
| AWS SES | 40 MB |
| SMTP | Whatever the server accepts |

For anything larger, upload the file somewhere and send a link instead. MailVolt does not
enforce these limits — the provider rejects the send and the reason appears in
`EmailResult.Error`.
