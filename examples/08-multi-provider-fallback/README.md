# 08 — Console · Multi-Provider Fallback

Resilience pattern: tries Resend as primary sender, falls back to SMTP on failure.

## Setup

1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your Resend API key and SMTP credentials
3. Set `MAILVOLT_RESEND__APIKEY=INVALID_KEY` env var to force fallback demo

## Run

```bash
dotnet run
```

## How it works

- `FallbackSender` wraps two `ISender` implementations
- On success of primary (Resend), returns immediately
- On failure, logs a warning and delegates to fallback (SMTP)
- If both fail, logs an error and returns the fallback failure

## What it shows

- Multi-provider resilience pattern
- Concrete sender registration by type (`SmtpSender`, `IResendSender`)
- Custom `ISender` decorator wrapping multiple transports
- No `--dry-run` — pure production resilience pattern
