# 05 — Minimal API · SMTP

HTTP endpoint that sends an email on POST /contact.

## Setup

1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your SMTP credentials (Mailtrap recommended for testing)

## Run

```bash
dotnet run
curl -X POST http://localhost:5000/contact \
  -H "Content-Type: application/json" \
  -d '{"name":"Denis","email":"d@example.com","message":"Hello!"}'
```

## Endpoints

| Method | Path      | Description                    |
|--------|-----------|--------------------------------|
| GET    | /health   | Health check                   |
| POST   | /contact  | Sends contact form via email   |

## What it shows

- SMTP transport with MailKit in a web context
- Minimal API endpoint pattern
- Environment-aware config with override support
