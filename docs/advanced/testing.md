# Testing

MailVolt provides first-class testing utilities via the `MailVolt.Testing` package to write reliable unit tests without sending real emails.

## Installation

```bash
dotnet add package MailVolt.Testing
```

## InMemorySender

Captures all sent emails in memory for assertion:

```csharp
using MailVolt.Testing;
```

### Registration

```csharp
builder.Services.AddMailVolt()
    .UseInMemoryTransport();
```

This registers `InMemorySender` as a singleton (so the same instance is available to both your app code and test assertions).

### API

```csharp
public sealed class InMemorySender : ISender
{
    public IReadOnlyList<SentEmail> SentEmails { get; }
    public int SentCount { get; }
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken ct = default);
    public void Clear();
}

public sealed record SentEmail(EmailMessage Email, DateTimeOffset SentAt);
```

### Unit Test Example

```csharp
[Fact]
public async Task Should_send_welcome_email()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddMailVolt().UseInMemoryTransport();
    services.AddScoped<WelcomeService>();

    var sp = services.BuildServiceProvider();
    var sender = sp.GetRequiredService<InMemorySender>();
    var service = sp.GetRequiredService<WelcomeService>();

    // Act
    await service.SendWelcomeEmailAsync("user@example.com");

    // Assert
    Assert.Single(sender.SentEmails);
    Assert.Contains(sender.SentEmails, e =>
        e.Email.To.Any(a => a.Address == "user@example.com"));
}
```

## FailingSender

Simulates send failures for error handling tests:

```csharp
public sealed class FailingSender(string? errorMessage = null) : ISender
{
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken ct = default)
        => Task.FromResult(EmailResult.Failure(errorMessage ?? "Simulated send failure"));
}
```

### Usage

```csharp
// Register as the sender
services.AddSingleton<ISender>(new FailingSender("Custom error"));

// Your service handles failure
var result = await emailService.SendAsync();
Assert.True(result.IsFailure);
Assert.Equal("Custom error", result.Error);
```

## FluentAssertions Extensions

The package includes custom assertions for `InMemorySender`:

```csharp
using MailVolt.Testing;
```

| Method | Description |
|---|---|
| `HaveCount(int)` | Asserts exact number of sent emails |
| `ContainEmailTo(string)` | Asserts at least one email was sent to the address |
| `ContainSubject(string)` | Asserts at least one email has the subject |
| `ContainHtmlBody(string)` | Asserts at least one email contains the HTML body |
| `HaveNoEmailsSent()` | Asserts no emails were sent |
| `ContainAttachment(string)` | Asserts at least one email has the attachment |

### Example

```csharp
[Fact]
public async Task Should_send_confirmation()
{
    var services = new ServiceCollection();
    services.AddMailVolt().UseInMemoryTransport();
    services.AddScoped<OrderService>();

    var sp = services.BuildServiceProvider();
    var sender = sp.GetRequiredService<InMemorySender>();
    var service = sp.GetRequiredService<OrderService>();

    await service.SendOrderConfirmationAsync("alice@example.com", 42);

    sender.Should().HaveCount(1);
    sender.Should().ContainEmailTo("alice@example.com");
    sender.Should().ContainSubject("Order #42 Confirmed");
    sender.Should().ContainHtmlBody("Thank you for your order");
}
```

## DI Setup for Tests

### xUnit + `WebApplicationFactory`

```csharp
public class EmailTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmailTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddMailVolt().UseInMemoryTransport();
            });
        });
    }

    [Fact]
    public async Task Registration_sends_welcome_email()
    {
        var client = _factory.CreateClient();
        await client.PostAsync("/register", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", "test@example.com")
        }));

        var sender = _factory.Services.GetRequiredService<InMemorySender>();
        sender.Should().ContainEmailTo("test@example.com");
    }
}
```

### Test Cleanup

Reset state between tests:

```csharp
public class EmailTests
{
    private readonly InMemorySender _sender;

    public EmailTests(InMemorySender sender)
    {
        _sender = sender;
        _sender.Clear();
    }

    // ...
}
```
