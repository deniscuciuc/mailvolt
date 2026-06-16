# 04 — Console · Resend + Batch

Sends a newsletter to 20 subscribers in a batch via Resend, using Liquid templates.

## Setup
1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your Resend API key

## Run
```bash
dotnet run                  # sends real email
dotnet run -- --dry-run     # prints result, no email sent
```

## What it shows
- Resend cloud transport
- Bulk/batch sending with `IBatchEmailSender`
- Concurrency throttling (`MaxConcurrency=5`, `DelayMs=100`)
- Continue-on-failure strategy
- Aggregated results (SentCount, FailedCount, TotalCount)
- `--dry-run` with InMemorySender
