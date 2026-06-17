# 01 — Console · SMTP + Razor

Sends a welcome email via SMTP using a Razor `.cshtml` template.

## Setup

1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your SMTP credentials (Mailtrap recommended for testing)

## Run

```bash
dotnet run                  # sends real email
dotnet run -- --dry-run     # prints result, no email sent
```

## What it shows

- SMTP transport with MailKit
- Razor template from file
- `--dry-run` with InMemorySender
