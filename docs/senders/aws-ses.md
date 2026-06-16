# AWS SES Transport

Sends emails via [Amazon Simple Email Service (SES)](https://aws.amazon.com/ses/).

## Installation

```bash
dotnet add package MailVolt.Transport.AwsSes
```

## Registration

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

Or bind from configuration:

```csharp
builder.Services.AddMailVolt()
    .UseAwsSesTransport(
        builder.Configuration.GetSection("MailVolt:AwsSes"));
```

## Options

| Property | Type | Default | Required |
|---|---|---|---|
| `AccessKeyId` | `string` | — | Yes |
| `SecretAccessKey` | `string` | — | Yes |
| `Region` | `string` | `"us-east-1"` | No |
| `ConfigurationSetName` | `string?` | `null` | No |

## Full Example

```csharp
builder.Services.AddMailVolt()
    .UseAwsSesTransport(options =>
    {
        options.AccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")!;
        options.SecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")!;
        options.Region = "eu-west-1";
        options.ConfigurationSetName = "my-config-set";
    });
```

```csharp
public class TransactionalService
{
    private readonly IEmailBuilder _builder;

    public TransactionalService(IEmailBuilder builder)
    {
        _builder = builder;
    }

    public async Task<EmailResult> SendReceiptAsync(string email)
    {
        return await _builder
            .From("receipts@example.com")
            .To(email)
            .Subject("Your Receipt")
            .HtmlBody("<h1>Thank you for your purchase</h1>")
            .SendAsync();
    }
}
```

## Configuration Section (`appsettings.json`)

```json
{
  "MailVolt": {
    "AwsSes": {
      "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
      "SecretAccessKey": "your-secret-key",
      "Region": "us-east-1",
      "ConfigurationSetName": "my-config-set"
    }
  }
}
```
