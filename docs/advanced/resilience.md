# Resilience

MailVolt HTTP-based transports (Mailgun, SendGrid, Resend, Postmark, Brevo) use `Microsoft.Extensions.Http.Resilience` for built-in HTTP resilience.

## Standard Resilience Pipeline

Transports register typed `HttpClient` instances with `AddStandardResilienceHandler()`:

```csharp
builder.Services
    .AddHttpClient<IMailgunSender, MailgunSender>()
    .AddStandardResilienceHandler();
```

This provides the following pipeline from `Microsoft.Extensions.Http.Resilience`:

| Layer | Strategy | Default Behavior |
|---|---|---|
| 1 | **Rate Limiter** | Protects the downstream API from excessive requests |
| 2 | **Total Request Timeout** | Overall timeout for the entire request pipeline |
| 3 | **Retry** | Retries transient failures with exponential backoff |
| 4 | **Circuit Breaker** | Opens the circuit when failure rate exceeds threshold |
| 5 | **Attempt Timeout** | Per-attempt timeout for individual requests |

## Customizing Resilience

Override the default pipeline per transport:

```csharp
builder.Services
    .AddHttpClient<IMailgunSender, MailgunSender>()
    .AddResilienceHandler("mailgun-pipeline", builder =>
    {
        // Retry
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            MaxDelay = TimeSpan.FromSeconds(30),
        });

        // Circuit breaker
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.2,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(15),
        });

        // Timeout per attempt
        builder.AddTimeout(new HttpTimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(10),
        });
    });
```

For SMTP transport, configure timeouts via the options:

```csharp
.UseSmtpTransport(options =>
{
    options.Host = "smtp.example.com";
    options.TimeoutMs = 15000; // 15 second timeout
})
```

## Transient Fault Handling

The retry policy in `AddStandardResilienceHandler` automatically retries on:

- HTTP 5xx server errors
- HTTP 408 Request Timeout
- HTTP 429 Too Many Requests
- Network/DNS failures
- `HttpRequestException`

## Resources

- [Microsoft.Extensions.Http.Resilience documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
