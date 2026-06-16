# 07 — Console · Inline Images (CID)

Sends an invoice email with a company logo embedded inline using CID (Content-ID) attachments.

## Setup

1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your SMTP credentials (Mailtrap recommended for testing)

## Run

```bash
dotnet run                  # sends real email with inline logo
dotnet run -- --dry-run     # uses InMemorySender, no email sent
```

## How it works

- The Razor template uses `<img src="cid:company-logo">` to reference the inline image
- The `Attach(...).FromFile(...).AsInlineImage("company-logo")` call attaches the PNG as a MIME inline resource
- The `ContentId` matches the `cid:` reference in the HTML

## What it shows

- Inline CID images in HTML email
- MailKit SMTP transport with `LinkedResource` support
- `IAttachmentBuilder` fluent API
- `--dry-run` with InMemorySender
