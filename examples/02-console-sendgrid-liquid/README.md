# 02 — Console · SendGrid + Liquid

Sends an order confirmation email via SendGrid using a Liquid template.

## Setup
1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your SendGrid API key

## Run
```bash
dotnet run                  # sends real email
dotnet run -- --dry-run     # prints result, no email sent
```

## What it shows
- SendGrid cloud transport
- Liquid templates (safer for user-generated content)
- Anonymous model with array iteration over items
- `--dry-run` with InMemorySender
