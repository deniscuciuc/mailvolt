# MailVolt.Transport.AwsSes

AWS SES transport for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Transport.AwsSes
```

## Quick start

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseAwsSesTransport(options =>
    {
        options.AccessKeyId = "AKIAIOSFODNN7EXAMPLE";
        options.SecretAccessKey = "your-secret-key";
        options.Region = "us-west-2";
    });
```

## Documentation

- [AWS SES Transport](../../docs/senders/aws-ses.md)
- [Getting Started](../../docs/getting-started.md)
