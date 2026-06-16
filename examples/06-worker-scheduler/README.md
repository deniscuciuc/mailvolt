# 06 — Worker Service · Scheduled Digest

Background worker that sends daily digest emails at 08:00 via SMTP.

## Setup

1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your SMTP credentials (Mailtrap recommended for testing)

## Run

```bash
dotnet run                    # sends real email at scheduled time
dotnet run -- --dry-run       # uses InMemorySender, no email sent
```

## What it shows

- `BackgroundService` with scheduled execution
- Liquid templates rendered via Fluid
- `IServiceScopeFactory` for resolving scoped `IEmailBuilder`
- `IBatchEmailSender` with MaxConcurrency and DelayMs
- `--dry-run` with InMemorySender
