# 09 — Testing Demo

Shows how to unit-test code that sends email using `InMemorySender` — no real emails, no network.

## Run tests

```bash
cd tests
dotnet test
```

## Key pattern

1. Register `.UseInMemoryTransport()` in test DI setup
2. Resolve `InMemorySender` as singleton — it captures all sent emails in memory
3. Use `_sender.Should().ContainEmailTo(...)` FluentAssertions extensions
4. Each test creates fresh services, so `InMemorySender` starts empty

## Tests

| Test | What it verifies |
|------|------------------|
| `Register_SendsWelcomeEmail_ToCorrectAddress` | Email sent to correct address with correct subject |
| `Register_SendsExactlyOneEmail` | Exactly one email sent per registration |
| `SendPasswordReset_UsesHighPriority` | Password reset emails use `EmailPriority.High` |
| `NoSideEffects_BetweenTests` | Fresh InMemorySender per test class |
