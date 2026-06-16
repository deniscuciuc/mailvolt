# 03 — Console · Mailgun + Handlebars

Sends a password reset email via Mailgun using a Handlebars template.

## Setup
1. Copy `appsettings.example.json` → `appsettings.json`
2. Fill in your Mailgun API key and domain

## Run
```bash
dotnet run                  # sends real email
dotnet run -- --dry-run     # prints result, no email sent
```

## What it shows
- Mailgun cloud transport
- Handlebars templates with `{{#if}}` conditionals
- Tags for categorization (`transactional`, `password-reset`)
- Priority set to High
- `--dry-run` with InMemorySender
