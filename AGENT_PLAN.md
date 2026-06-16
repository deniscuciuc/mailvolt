# AGENT_PLAN.md — MailVolt Full Project Implementation

> **For AI agents (Claude Code / GitHub Copilot Agent / OpenAI Codex)**
> Read this file completely before writing a single line of code.
> Implement phases in exact order. Run `dotnet build` and `dotnet test` after each phase.
> Do NOT skip phases. Do NOT refactor prematurely. Do NOT add features not listed here.

---

## Project Overview

**MailVolt** is a modern, fully-async .NET email library that replaces the abandoned FluentEmail.
It solves every known issue in FluentEmail and adds first-class support for modern providers, templates,
batch sending, resilience, and testing infrastructure.

- **Repository:** `github.com/deniscuciuc/mailvolt`
- **NuGet namespace:** `MailVolt.*`
- **License:** MIT
- **Target frameworks:** `net8.0;net9.0;net10.0` — **net10.0 is the primary target (LTS until Nov 2028)**
- **Language:** C# 14 (ships with .NET 10), nullable enabled, implicit usings enabled
- **Author:** Denis Cuciuc <denis.cuciuc@zelqonworks.com>
- **Architecture:** Clean modular packages, DI-first, async-only, no static state

### .NET version strategy

| Version | Type | EOL        | Role in MailVolt               |
|---------|------|------------|---------------------------------|
| .NET 10 | LTS  | Nov 2028   | **Primary target** — all new C# 14 features used here |
| .NET 9  | STS  | Nov 2026   | Supported, no C# 14-only code   |
| .NET 8  | LTS  | Nov 2026   | Minimum supported, broad compat |

Write all source code targeting C# 14 features (available on net10.0). Use `#if NET10_0_OR_GREATER`
preprocessor guards ONLY when a feature is unavailable on net8/net9 at runtime — not for language
features (those compile down). Prefer C# 14 idioms over older patterns throughout.

---

## What NOT to do

- ❌ Do NOT use `System.Net.Mail.SmtpClient` — use MailKit
- ❌ Do NOT add `.Send()` sync methods — async-only API
- ❌ Do NOT use RazorLight — use native `Microsoft.AspNetCore.Mvc.Razor`
- ❌ Do NOT register `ISender` as Scoped — use Transient / IHttpClientFactory typed clients
- ❌ Do NOT use `HttpClient` directly — always via `IHttpClientFactory`
- ❌ Do NOT add features not listed in this plan
- ❌ Do NOT use `GetAwaiter().GetResult()` anywhere
- ❌ Do NOT add `[Obsolete]` shims for FluentEmail compatibility
- ❌ Do NOT create static `Email.DefaultSender` — DI only

---

## C# 14 Features to Use (net10.0 is primary)

.NET 10 ships C# 14. Use these features throughout the codebase — they are the idiomatic
modern style, not optional polish.

### Extension members (headline feature)

Use for fluent API sugar and DI extension chains. Replaces old static extension method classes
with a cleaner `extension` block syntax:

```csharp
// ✅ C# 14 — extension properties + static members on ISender
extension(ISender sender)
{
    // Extension property on ISender
    public bool IsReliable => sender is not FailingSender;
}

// ✅ C# 14 — extension block for IEmailBuilder (add helpers to the interface)
extension(IEmailBuilder builder)
{
    public IEmailBuilder ToMany(params string[] addresses)
    {
        foreach (var a in addresses) builder.To(a);
        return builder;
    }
    
    public IEmailBuilder HighPriority() => builder.Priority(EmailPriority.High);
    public IEmailBuilder LowPriority()  => builder.Priority(EmailPriority.Low);
}
```

### Null-conditional assignment

```csharp
// ✅ C# 14
options?.DefaultFromAddress = "noreply@example.com";

// ❌ Old style
if (options != null) options.DefaultFromAddress = "noreply@example.com";
```

### Field-backed properties (field keyword)

```csharp
// ✅ C# 14 — use field keyword in auto-property customization
public string Subject
{
    get;
    set => field = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
}

// ❌ Old style — needed a separate backing field
private string _subject = string.Empty;
public string Subject
{
    get => _subject;
    set => _subject = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
}
```

### nameof for unbound generics

```csharp
// ✅ C# 14 — useful in template renderers and error messages
var name = nameof(ITemplateRenderer<>);  // "ITemplateRenderer"

// Useful in generic error messages:
throw new InvalidOperationException(
    $"Template renderer {nameof(ITemplateRenderer<>)} is not registered.");
```

### Lambda parameter modifiers without type

```csharp
// ✅ C# 14 — cleaner in batch processing
var results = emails.Select(async (ref email, ct) => await sender.SendAsync(email, ct));
```

### Implicit Span<T> conversions

```csharp
// ✅ C# 14 — use in binary/attachment processing
ReadOnlySpan<byte> attachmentBytes = fileBytes;  // no explicit cast needed
```

### Where to apply C# 14 features

| Feature | Where to use in MailVolt |
|---------|---------------------------|
| Extension members | `IEmailBuilder` helper extensions, `ISender` extensions, `InMemorySender` assertion helpers |
| `field` keyword | `EmailMessage` property setters with validation, `SmtpSenderOptions`, all Options classes |
| Null-conditional assignment | Options defaults, nullable configuration fallbacks |
| `nameof` unbound generics | Error messages in template renderers, DI registration logs |
| Implicit Span conversions | Attachment byte processing in `AttachmentBuilder.FromBytes()` |

> **Note for net8/net9 targets:** All C# 14 language features compile down to IL that runs on
> net8.0 and net9.0. NO runtime guards needed for these language features. Guards are only needed
> for NET10-specific *runtime APIs* (e.g. new BCL methods). Check before adding `#if NET10_0_OR_GREATER`.

---

## Repository Structure (create exactly this)

```
mailvolt/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml
│   │   ├── release.yml
│   │   └── publish-nuget.yml
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.md
│   │   └── feature_request.md
│   └── PULL_REQUEST_TEMPLATE.md
├── src/
│   ├── MailVolt.Core/
│   ├── MailVolt.Transport.Smtp/
│   ├── MailVolt.Transport.Mailgun/
│   ├── MailVolt.Transport.SendGrid/
│   ├── MailVolt.Transport.Resend/
│   ├── MailVolt.Transport.Postmark/
│   ├── MailVolt.Transport.AzureEmail/
│   ├── MailVolt.Transport.Brevo/
│   ├── MailVolt.Transport.AwsSes/
│   ├── MailVolt.Templates.Razor/
│   ├── MailVolt.Templates.Liquid/
│   ├── MailVolt.Templates.Handlebars/
│   └── MailVolt.Testing/
├── tests/
│   ├── MailVolt.Core.Tests/
│   ├── MailVolt.Transport.Tests/
│   ├── MailVolt.Templates.Tests/
│   └── MailVolt.Integration.Tests/
├── samples/
│   ├── Sample.AspNetCore/
│   ├── Sample.WorkerService/
│   └── Sample.MinimalApi/
├── docs/
│   ├── getting-started.md
│   ├── senders/
│   │   ├── smtp.md
│   │   ├── mailgun.md
│   │   ├── sendgrid.md
│   │   ├── resend.md
│   │   ├── postmark.md
│   │   ├── azure.md
│   │   ├── brevo.md
│   │   └── aws-ses.md
│   ├── templates/
│   │   ├── razor.md
│   │   ├── liquid.md
│   │   └── handlebars.md
│   └── advanced/
│       ├── batch-sending.md
│       ├── resilience.md
│       └── testing.md
├── Directory.Build.props
├── Directory.Packages.props
├── MailVolt.sln
├── README.md
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── SECURITY.md
├── LICENSE
└── .editorconfig
```

---

## Phase 0 — Repository Bootstrap

### 0.1 Root Files

**`Directory.Build.props`** — shared MSBuild properties for ALL projects:
```xml
<Project>
  <PropertyGroup>
    <!--
      net10.0 = PRIMARY target (LTS until Nov 2028, C# 14, best perf)
      net9.0  = STS, supported until Nov 2026
      net8.0  = LTS, minimum bar for broad ecosystem compat
    -->
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- Explicitly pin C# 14 — do NOT use 'latest' to avoid accidental preview features -->
    <LangVersion>14</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <!-- AOT / trimming annotations -->
    <IsAotCompatible Condition="'$(TargetFramework)' == 'net10.0'">true</IsAotCompatible>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <Authors>Denis Cuciuc</Authors>
    <Company>Denis Cuciuc</Company>
    <Copyright>Copyright © 2026 Denis Cuciuc</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/deniscuciuc/mailvolt</PackageProjectUrl>
    <RepositoryUrl>https://github.com/deniscuciuc/mailvolt</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>email;mail;smtp;sendgrid;mailgun;resend;postmark;dotnet;csharp;net10</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

**`Directory.Packages.props`** — centralized NuGet versions (CPM):
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- MailKit / MimeKit — net10 compatible -->
    <PackageVersion Include="MailKit" Version="4.11.0" />
    <PackageVersion Include="MimeKit" Version="4.11.0" />
    <!-- HTTP Resilience — align with .NET 10 -->
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />
    <!-- Azure -->
    <PackageVersion Include="Azure.Communication.Email" Version="1.1.0" />
    <!-- AWS -->
    <PackageVersion Include="AWSSDK.SimpleEmailV2" Version="3.7.401.0" />
    <!-- Templates -->
    <PackageVersion Include="Fluid.Core" Version="2.14.0" />
    <PackageVersion Include="Handlebars.Net" Version="2.1.6" />
    <!-- Razor: use AspNetCore meta-package for net10 -->
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Razor" Version="10.0.0" />
    <!-- DI & Extensions — all aligned to .NET 10 -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="FluentAssertions" Version="8.2.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <!-- SourceLink -->
    <PackageVersion Include="Microsoft.SourceLink.GitHub" Version="8.0.0" />
  </ItemGroup>
</Project>
```

> ⚠️ **Agent note:** Before running `dotnet restore`, verify all versions above exist on NuGet.
> Run `dotnet package search <PackageId>` or check nuget.org for the latest stable version
> compatible with `net10.0`. Update any version that doesn't exist yet.

**`.editorconfig`**:
```ini
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
trim_trailing_whitespace = true
insert_final_newline = true

[*.{csproj,props,targets}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

**`LICENSE`** — MIT License with year 2026 and author Denis Cuciuc.

**`CODE_OF_CONDUCT.md`** — Contributor Covenant v2.1.

**`SECURITY.md`** — responsible disclosure policy, contact: denis.cuciuc@zelqonworks.com.

**`.gitignore`** — standard dotnet .gitignore (bin/, obj/, *.user, .vs/, .idea/, etc.)

### 0.2 Solution File

Create `MailVolt.sln` and add all src + tests + samples projects.

---

## Phase 1 — MailVolt.Core

This is the foundation. Everything else depends on it. Get it right.

### 1.1 Project File

```xml
<!-- src/MailVolt.Core/MailVolt.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>MailVolt.Core</PackageId>
    <Version>1.0.0</Version>
    <Description>Core abstractions and fluent builder for MailVolt — modern .NET email library</Description>
    <NoWarn>CS1591</NoWarn><!-- XML doc warnings only for public API -->
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
    <None Include="../../README.md" Pack="true" PackagePath="/" />
  </ItemGroup>
</Project>
```

### 1.2 Domain Models

**`Models/EmailAddress.cs`**:
```csharp
namespace MailVolt.Core.Models;

/// <summary>Represents an RFC 5321 email address with optional display name.</summary>
public sealed record EmailAddress(string Address, string? DisplayName = null)
{
    public override string ToString() =>
        DisplayName is { Length: > 0 } name ? $"{name} <{Address}>" : Address;
        
    /// <summary>Implicitly converts a plain email string to EmailAddress.</summary>
    public static implicit operator EmailAddress(string address) => new(address);
}
```

**`Models/EmailAttachment.cs`**:
```csharp
namespace MailVolt.Core.Models;

public sealed class EmailAttachment
{
    public required string FileName { get; init; }
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
    
    /// <summary>
    /// When set, this attachment is embedded inline with the given Content-ID.
    /// Use &lt;img src="cid:{ContentId}" /&gt; in the HTML body.
    /// </summary>
    public string? ContentId { get; init; }
    
    /// <summary>True if this is an inline CID attachment (embedded image).</summary>
    public bool IsInline => ContentId is not null;
}
```

**`Models/EmailPriority.cs`**:
```csharp
namespace MailVolt.Core.Models;

public enum EmailPriority { Low, Normal, High }
```

**`Models/EmailMessage.cs`**:
```csharp
namespace MailVolt.Core.Models;

/// <summary>Immutable email message model. Built via <see cref="IEmailBuilder"/>.</summary>
public sealed class EmailMessage
{
    public EmailAddress From { get; init; } = null!;
    public IReadOnlyList<EmailAddress> To { get; init; } = [];
    public IReadOnlyList<EmailAddress> Cc { get; init; } = [];
    public IReadOnlyList<EmailAddress> Bcc { get; init; } = [];
    public IReadOnlyList<EmailAddress> ReplyTo { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public string? HtmlBody { get; init; }
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;
    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Tags { get; init; } = [];
}
```

**`Models/EmailResult.cs`**:
```csharp
namespace MailVolt.Core.Models;

/// <summary>Result of a send operation. Never throws — check IsSuccess.</summary>
public sealed class EmailResult
{
    private EmailResult() { }
    
    public bool IsSuccess { get; private init; }
    public bool IsFailure => !IsSuccess;
    public string? MessageId { get; private init; }
    public string? Error { get; private init; }
    public Exception? Exception { get; private init; }
    
    public static EmailResult Success(string? messageId = null) =>
        new() { IsSuccess = true, MessageId = messageId };
        
    public static EmailResult Failure(string error, Exception? exception = null) =>
        new() { IsSuccess = false, Error = error, Exception = exception };
        
    public override string ToString() =>
        IsSuccess ? $"Success(MessageId={MessageId})" : $"Failure({Error})";
}
```

**`Models/BatchEmailResult.cs`**:
```csharp
namespace MailVolt.Core.Models;

public sealed class BatchEmailResult
{
    public int TotalCount { get; init; }
    public int SentCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<(EmailMessage Email, EmailResult Result)> Results { get; init; } = [];
    public bool HasFailures => FailedCount > 0;
}
```

### 1.3 Interfaces

**`Interfaces/ISender.cs`**:
```csharp
namespace MailVolt.Core.Interfaces;

/// <summary>
/// Registered as Transient. Implementations MUST be thread-safe or stateless.
/// </summary>
public interface ISender
{
    Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default);
}
```

**`Interfaces/ITemplateRenderer.cs`**:
```csharp
namespace MailVolt.Core.Interfaces;

public interface ITemplateRenderer
{
    /// <summary>Render a template with the given model and return the HTML/text string.</summary>
    Task<string> RenderAsync<TModel>(string templatePathOrKey, TModel model, CancellationToken cancellationToken = default);
}
```

**`Interfaces/IEmailBuilder.cs`**:
```csharp
namespace MailVolt.Core.Interfaces;

/// <summary>
/// Fluent builder for constructing and sending EmailMessage instances.
/// Registered as Transient — get a fresh instance per use.
/// </summary>
public interface IEmailBuilder
{
    IEmailBuilder From(string address, string? displayName = null);
    IEmailBuilder To(string address, string? displayName = null);
    IEmailBuilder To(IEnumerable<EmailAddress> addresses);
    IEmailBuilder Cc(string address, string? displayName = null);
    IEmailBuilder Bcc(string address, string? displayName = null);
    IEmailBuilder ReplyTo(string address, string? displayName = null);
    IEmailBuilder Subject(string subject);
    IEmailBuilder Body(string body, bool isHtml = false);
    IEmailBuilder TextBody(string text);
    IEmailBuilder HtmlBody(string html);
    IEmailBuilder Priority(EmailPriority priority);
    IEmailBuilder Tag(params string[] tags);
    IEmailBuilder Header(string name, string value);
    IEmailBuilder Attach(Action<IAttachmentBuilder> configure);
    
    /// <summary>Render a template and set as HTML body.</summary>
    IEmailBuilder UsingTemplate<TModel>(string templatePathOrKey, TModel model);
    
    /// <summary>Build the EmailMessage without sending.</summary>
    Task<EmailMessage> BuildAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Build and send the email.</summary>
    Task<EmailResult> SendAsync(CancellationToken cancellationToken = default);
}
```

**`Interfaces/IAttachmentBuilder.cs`**:
```csharp
namespace MailVolt.Core.Interfaces;

public interface IAttachmentBuilder
{
    IAttachmentBuilder FromFile(string filePath);
    IAttachmentBuilder FromStream(Stream stream, string fileName, string contentType);
    IAttachmentBuilder FromBytes(byte[] bytes, string fileName, string contentType);
    
    /// <summary>Embed as inline CID image. Use &lt;img src="cid:{contentId}" /&gt; in HTML.</summary>
    IAttachmentBuilder AsInlineImage(string contentId);
    
    /// <summary>Override auto-detected content type.</summary>
    IAttachmentBuilder WithContentType(string contentType);
    
    /// <summary>Override display file name.</summary>
    IAttachmentBuilder WithFileName(string fileName);
}
```

**`Interfaces/IBatchEmailSender.cs`**:
```csharp
namespace MailVolt.Core.Interfaces;

public interface IBatchEmailSender
{
    Task<BatchEmailResult> SendBatchAsync(
        IEnumerable<EmailMessage> emails,
        BatchSendOptions? options = null,
        CancellationToken cancellationToken = default);
}

public sealed class BatchSendOptions
{
    /// <summary>Maximum concurrent sends. Default: 5.</summary>
    public int MaxConcurrency { get; init; } = 5;
    
    /// <summary>Delay between sends in ms, for rate limiting. Default: 0.</summary>
    public int DelayMs { get; init; } = 0;
    
    /// <summary>Continue on failure or stop. Default: ContinueOnFailure.</summary>
    public BatchFailureStrategy FailureStrategy { get; init; } = BatchFailureStrategy.ContinueOnFailure;
}

public enum BatchFailureStrategy { ContinueOnFailure, StopOnFirstFailure }
```

### 1.4 Options

**`Options/MailVoltOptions.cs`**:
```csharp
namespace MailVolt.Core.Options;

public sealed class MailVoltOptions
{
    public const string SectionName = "MailVolt";
    
    /// <summary>Default From address used when none is set on the builder.</summary>
    public string? DefaultFromAddress { get; set; }
    
    /// <summary>Default From display name.</summary>
    public string? DefaultFromDisplayName { get; set; }
}
```

### 1.5 Implementation — EmailBuilder

**`EmailBuilder.cs`**:
Implement `IEmailBuilder` fully. Key points:
- Store state in private mutable fields
- In `SendAsync()`: call `BuildAsync()` then `ISender.SendAsync()`
- In `BuildAsync()`: if `UsingTemplate` was called, await `ITemplateRenderer.RenderAsync()` and set HtmlBody
- Auto-detect MIME type from file extension using a static dictionary (or `MimeTypes.GetMimeType(path)` from MimeKit if available — import it)
- Apply `MailVoltOptions.DefaultFromAddress` if no From was set
- Validate: throw `InvalidOperationException` with clear message if To is empty or Subject is empty

**`AttachmentBuilder.cs`**:
Implement `IAttachmentBuilder`. Key points:
- `FromFile(path)`: read file, auto-detect MIME type from extension using dictionary
  ```csharp
  private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
  {
      [".pdf"] = "application/pdf",
      [".png"] = "image/png",
      [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
      [".gif"] = "image/gif",  [".webp"] = "image/webp",
      [".svg"] = "image/svg+xml",
      [".txt"] = "text/plain", [".html"] = "text/html",
      [".csv"] = "text/csv",   [".xml"] = "application/xml",
      [".json"] = "application/json",
      [".zip"] = "application/zip",
      [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  };
  ```
- Use `FileStream` with `FileShare.Read` and dispose it only AFTER message is fully built/sent
- `AsInlineImage(contentId)`: set ContentId property on built attachment

### 1.6 Implementation — BatchEmailSender

**`BatchEmailSender.cs`**:
```csharp
// Uses SemaphoreSlim for concurrency control
// Uses Task.WhenAll with chunked batches
// Respects CancellationToken between batches
// Returns BatchEmailResult with per-email results
```

### 1.7 DI Extensions

**`DependencyInjection/MailVoltServiceCollectionExtensions.cs`**:
```csharp
namespace MailVolt.Core.DependencyInjection;

public static class MailVoltServiceCollectionExtensions
{
    /// <summary>
    /// Adds MailVolt core services. Chain with .UseSmtpTransport(), .UseSendGridTransport(), etc.
    /// </summary>
    public static MailVoltBuilder AddMailVolt(
        this IServiceCollection services,
        Action<MailVoltOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure<MailVoltOptions>(configure);
        
        services.AddTransient<IEmailBuilder, EmailBuilder>();
        services.AddTransient<IBatchEmailSender, BatchEmailSender>();
        
        return new MailVoltBuilder(services);
    }
    
    /// <summary>Adds MailVolt with configuration from IConfiguration section "MailVolt".</summary>
    public static MailVoltBuilder AddMailVolt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MailVoltOptions>(configuration.GetSection(MailVoltOptions.SectionName));
        services.AddTransient<IEmailBuilder, EmailBuilder>();
        services.AddTransient<IBatchEmailSender, BatchEmailSender>();
        return new MailVoltBuilder(services);
    }
}

/// <summary>Builder returned by AddMailVolt() for chaining transport/template registration.</summary>
public sealed class MailVoltBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services;
}
```

---

## Phase 2 — MailVolt.Transport.Smtp

**Package:** `MailVolt.Transport.Smtp`

Dependencies:
```xml
<PackageReference Include="MailKit" />
<PackageReference Include="MimeKit" />
```

### 2.1 Options

**`SmtpSenderOptions.cs`**:
```csharp
public sealed class SmtpSenderOptions
{
    public const string SectionName = "MailVolt:Smtp";
    public required string Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTlsWhenAvailable;
    public int TimeoutMs { get; set; } = 30_000;
    
    /// <summary>OAuth2 token provider. When set, overrides Username/Password.</summary>
    public Func<CancellationToken, Task<string>>? OAuth2TokenProvider { get; set; }
}
```

### 2.2 SmtpSender

**`SmtpSender.cs`**:
- Implements `ISender`
- Creates a NEW `MailKitClient` per `SendAsync()` call — NEVER reuse across calls
- Builds `MimeMessage` from `EmailMessage`:
  - Sets `From`, `To`, `Cc`, `Bcc`, `ReplyTo`
  - Sets `Subject`
  - Uses `BodyBuilder` for text + html parts
  - Adds regular attachments via `builder.Attachments.Add()`
  - Adds inline images via `builder.LinkedResources.Add()` with `ContentId`
  - Adds custom headers
  - Sets priority via `X-Priority` header
- Connects with specified `SecureSocketOptions`
- Authenticates: XOAUTH2 if OAuth2TokenProvider set, else PLAIN/LOGIN
- Sends, disconnects, disposes
- Returns `EmailResult.Success(messageId)` or `EmailResult.Failure(error, exception)`
- Wraps everything in try/catch

### 2.3 DI Extension

```csharp
public static class SmtpTransportExtensions
{
    public static MailVoltBuilder UseSmtpTransport(
        this MailVoltBuilder builder,
        Action<SmtpSenderOptions> configure)
    {
        builder.Services.Configure<SmtpSenderOptions>(configure);
        builder.Services.AddTransient<ISender, SmtpSender>();
        return builder;
    }
    
    public static MailVoltBuilder UseSmtpTransport(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        builder.Services.Configure<SmtpSenderOptions>(
            configuration.GetSection(SmtpSenderOptions.SectionName));
        builder.Services.AddTransient<ISender, SmtpSender>();
        return builder;
    }
}
```

---

## Phase 3 — MailVolt.Transport.Mailgun

**Package:** `MailVolt.Transport.Mailgun`

Dependencies: `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience`

### 3.1 Options

```csharp
public sealed class MailgunSenderOptions
{
    public const string SectionName = "MailVolt:Mailgun";
    public required string ApiKey { get; set; }
    public required string Domain { get; set; }
    public string BaseUrl { get; set; } = "https://api.mailgun.net/v3";
    
    /// <summary>When set, use Mailgun native template instead of HTML body.</summary>
    public bool UseNativeTemplates { get; set; } = false;
}
```

### 3.2 MailgunSender

- Implements `ISender`
- Injected via `IHttpClientFactory` typed client
- Sends `multipart/form-data` POST to `{BaseUrl}/{Domain}/messages`
- Maps ALL EmailMessage fields:
  - `from`, `to`, `cc`, `bcc`, `subject`, `text`, `html`
  - `h:Reply-To` for ReplyTo addresses (fix for long-standing FluentEmail issue)
  - `h:X-Priority` for priority
  - Custom headers as `h:{name}` fields
  - `o:tag` for tags
  - Attachments as multipart file parts
  - Inline images as `inline` parts with `Content-ID`
- Returns MessageId from response JSON `{"id": "<...@mailgun.org>"}`
- Full error handling with `MailgunErrorResponse` deserialization

### 3.3 DI Extension

```csharp
public static MailVoltBuilder UseMailgunTransport(
    this MailVoltBuilder builder,
    Action<MailgunSenderOptions> configure)
{
    builder.Services.Configure<MailgunSenderOptions>(configure);
    builder.Services
        .AddHttpClient<IMailgunSender, MailgunSender>()
        .AddStandardResilienceHandler();
    builder.Services.AddTransient<ISender>(sp => sp.GetRequiredService<IMailgunSender>());
    return builder;
}
```

---

## Phase 4 — MailVolt.Transport.SendGrid

**Package:** `MailVolt.Transport.SendGrid`

### 4.1 Options

```csharp
public sealed class SendGridSenderOptions
{
    public const string SectionName = "MailVolt:SendGrid";
    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.sendgrid.com";
    
    /// <summary>When true, use SendGrid dynamic transactional templates.</summary>
    public bool UseDynamicTemplates { get; set; } = false;
    public string? SandboxMode { get; set; }
}
```

### 4.2 SendGridSender

- Uses SendGrid v3 `/v3/mail/send` JSON API
- Builds JSON payload as C# record types (do NOT use Newtonsoft.Json, use System.Text.Json)
- Supports:
  - Multiple personalizations (To/Cc/Bcc)
  - HTML + Text body
  - Regular attachments (base64 encoded content)
  - **Inline attachments** (disposition: "inline", content_id set) — fix for unmerged FluentEmail PR
  - Custom headers
  - Tags as `categories`
  - Dynamic template data
  - Custom host via `BaseUrl`  — fix for unmerged FluentEmail PR
- Returns `x-message-id` response header as MessageId

---

## Phase 5 — MailVolt.Transport.Resend

**Package:** `MailVolt.Transport.Resend`

- Resend REST API v1: `POST https://api.resend.com/emails`
- JSON payload with `from`, `to`, `cc`, `bcc`, `reply_to`, `subject`, `html`, `text`
- Attachments: base64 `content` field
- Returns `{ "id": "..." }` as MessageId
- Batch API: `POST https://api.resend.com/emails/batch` — use in `IBatchEmailSender` implementation for Resend
- Auth: `Authorization: Bearer {ApiKey}`

```csharp
public sealed class ResendSenderOptions
{
    public const string SectionName = "MailVolt:Resend";
    public required string ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.resend.com";
}
```

---

## Phase 6 — MailVolt.Transport.Postmark

**Package:** `MailVolt.Transport.Postmark`

- API: `POST https://api.postmarkapp.com/email`
- Auth: `X-Postmark-Server-Token: {ApiKey}`
- Content-Type: `application/json`
- JSON fields: `From`, `To`, `Cc`, `Bcc`, `ReplyTo`, `Subject`, `HtmlBody`, `TextBody`, `Tag`, `Attachments`, `Headers`
- Attachments: `{ Name, Content (base64), ContentType, ContentID }`
- Message stream support: `MessageStream` option
- Returns `MessageID` from response

```csharp
public sealed class PostmarkSenderOptions
{
    public const string SectionName = "MailVolt:Postmark";
    public required string ApiKey { get; set; }
    public string MessageStream { get; set; } = "outbound";
    public string BaseUrl { get; set; } = "https://api.postmarkapp.com";
}
```

---

## Phase 7 — MailVolt.Transport.AzureEmail

**Package:** `MailVolt.Transport.AzureEmail`

Dependencies: `Azure.Communication.Email`

- Uses `EmailClient` from Azure SDK
- Constructor injection of `EmailClient` via factory from options
- Maps `EmailMessage` to `Azure.Communication.Email.EmailMessage`
- Polls for send status or uses fire-and-forget based on option
- Returns operation ID as MessageId

```csharp
public sealed class AzureEmailSenderOptions
{
    public const string SectionName = "MailVolt:Azure";
    public required string ConnectionString { get; set; }
}
```

---

## Phase 8 — MailVolt.Transport.Brevo

**Package:** `MailVolt.Transport.Brevo`

- API: `POST https://api.brevo.com/v3/smtp/email`
- Auth: `api-key: {ApiKey}` header
- JSON: `sender`, `to`, `cc`, `bcc`, `replyTo`, `subject`, `htmlContent`, `textContent`
- Attachments: `{ name, content (base64) }`

```csharp
public sealed class BrevoSenderOptions
{
    public const string SectionName = "MailVolt:Brevo";
    public required string ApiKey { get; set; }
}
```

---

## Phase 9 — MailVolt.Transport.AwsSes

**Package:** `MailVolt.Transport.AwsSes`

Dependencies: `AWSSDK.SimpleEmailV2`

- Uses `AmazonSimpleEmailServiceV2Client`
- Sends via `SendEmailAsync` with `EmailContent` (simple) or `RawMessage` for attachments
- For messages WITH attachments: serialize as raw MIME using MimeKit
- For messages WITHOUT attachments: use `Simple` content type (faster path)

```csharp
public sealed class AwsSesSenderOptions
{
    public const string SectionName = "MailVolt:AwsSes";
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string? ConfigurationSetName { get; set; }
}
```

---

## Phase 10 — MailVolt.Templates.Razor

**Package:** `MailVolt.Templates.Razor`

Dependencies: `Microsoft.AspNetCore.Mvc.Razor`

**CRITICAL: Do NOT use RazorLight. Use native Razor SDK.**

- Register `IRazorViewEngine`, `ITempDataProvider`, `IActionContextAccessor`
- Implement `ITemplateRenderer` using `IRazorViewEngine.FindView()` + `IHtmlHelper`
- Support:
  - `.cshtml` files by path
  - Embedded resource views (pass assembly)
  - Layout pages (`_Layout.cshtml`)
  - Partial views
  - `@inject` — works natively
  - Strongly-typed models via `@model TModel`

```csharp
public sealed class RazorTemplateOptions
{
    /// <summary>Root directory for view resolution. Default: current directory.</summary>
    public string RootDirectory { get; set; } = Directory.GetCurrentDirectory();
}
```

DI Extension:
```csharp
public static MailVoltBuilder UseRazorTemplates(
    this MailVoltBuilder builder,
    Action<RazorTemplateOptions>? configure = null)
{
    // Add MVC services needed for Razor
    builder.Services.AddMvcCore().AddRazorViewEngine();
    builder.Services.AddTransient<ITemplateRenderer, RazorTemplateRenderer>();
    if (configure is not null)
        builder.Services.Configure<RazorTemplateOptions>(configure);
    return builder;
}
```

---

## Phase 11 — MailVolt.Templates.Liquid

**Package:** `MailVolt.Templates.Liquid`

Dependencies: `Fluid.Core`

- Implements `ITemplateRenderer` using `FluidParser`
- Parses Liquid template string, renders with `TemplateContext` populated from model
- Supports file-based and string-based templates
- Thread-safe (Fluid parser is stateless)

```csharp
public static MailVoltBuilder UseLiquidTemplates(this MailVoltBuilder builder)
{
    builder.Services.AddTransient<ITemplateRenderer, LiquidTemplateRenderer>();
    return builder;
}
```

---

## Phase 12 — MailVolt.Templates.Handlebars

**Package:** `MailVolt.Templates.Handlebars`

Dependencies: `Handlebars.Net`

- Implements `ITemplateRenderer` using `Handlebars.Compile()`
- Compiles template once (cache by templateKey), renders with model as data context
- Thread-safe template compilation cache via `ConcurrentDictionary`

---

## Phase 13 — MailVolt.Testing

**Package:** `MailVolt.Testing`

This package enables unit testing of email-sending code without network calls.

### 13.1 InMemorySender

```csharp
/// <summary>
/// ISender implementation that captures emails in memory instead of sending.
/// Thread-safe. Register as Singleton in test DI.
/// </summary>
public sealed class InMemorySender : ISender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();
    
    public IReadOnlyList<SentEmail> SentEmails => _sent.ToList();
    public int SentCount => _sent.Count;
    
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(email, DateTimeOffset.UtcNow));
        return Task.FromResult(EmailResult.Success(Guid.NewGuid().ToString()));
    }
    
    public void Clear() => _sent.Clear();
}

public sealed record SentEmail(EmailMessage Email, DateTimeOffset SentAt);
```

### 13.2 FailingSender

```csharp
/// <summary>ISender that always fails — for testing error handling.</summary>
public sealed class FailingSender(string? errorMessage = null) : ISender
{
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default) =>
        Task.FromResult(EmailResult.Failure(errorMessage ?? "Simulated send failure"));
}
```

### 13.3 FluentAssertions Extensions

```csharp
public static class InMemorySenderAssertions
{
    public static SentEmailAssertions Should(this InMemorySender sender) =>
        new(sender);
}

public sealed class SentEmailAssertions(InMemorySender sender)
{
    public SentEmailAssertions HaveCount(int count)
    {
        sender.SentCount.Should().Be(count, "expected {0} emails to be sent", count);
        return this;
    }
    
    public SentEmailAssertions ContainEmailTo(string address)
    {
        sender.SentEmails.Should()
            .Contain(s => s.Email.To.Any(a => a.Address.Equals(address, StringComparison.OrdinalIgnoreCase)),
            "expected an email to {0}", address);
        return this;
    }
    
    public SentEmailAssertions ContainSubject(string subject)
    {
        sender.SentEmails.Should()
            .Contain(s => s.Email.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase),
            "expected subject containing '{0}'", subject);
        return this;
    }
    
    public SentEmailAssertions ContainHtmlBody(string content)
    {
        sender.SentEmails.Should()
            .Contain(s => s.Email.HtmlBody != null && 
                         s.Email.HtmlBody.Contains(content, StringComparison.OrdinalIgnoreCase),
            "expected HTML body containing '{0}'", content);
        return this;
    }
    
    public SentEmailAssertions HaveNoEmailsSent()
    {
        sender.SentCount.Should().Be(0, "expected no emails to be sent");
        return this;
    }
    
    public SentEmailAssertions ContainAttachment(string fileName)
    {
        sender.SentEmails.Should()
            .Contain(s => s.Email.Attachments.Any(a => a.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)),
            "expected attachment '{0}'", fileName);
        return this;
    }
}
```

### 13.4 DI Extension

```csharp
public static class TestingExtensions
{
    /// <summary>
    /// Replaces the real ISender with InMemorySender for testing.
    /// Register as Singleton so the same instance can be inspected after sending.
    /// </summary>
    public static MailVoltBuilder UseInMemoryTransport(this MailVoltBuilder builder)
    {
        builder.Services.AddSingleton<InMemorySender>();
        builder.Services.AddSingleton<ISender>(sp => sp.GetRequiredService<InMemorySender>());
        return builder;
    }
}
```

---

## Phase 14 — Tests

### 14.1 MailVolt.Core.Tests

Write xUnit tests for:
- `EmailBuilder` — all builder methods
- `EmailBuilder.BuildAsync()` — validates required fields, throws on missing To/Subject
- `EmailBuilder.SendAsync()` — calls ISender.SendAsync with correct EmailMessage
- `AttachmentBuilder.FromFile()` — correct MIME type detection for each extension
- `AttachmentBuilder.AsInlineImage()` — ContentId is set, IsInline is true
- `BatchEmailSender` — sends all, respects concurrency limit, handles failures per strategy
- `EmailResult` — Success/Failure factory methods
- `EmailAddress` — implicit string conversion, ToString with/without display name

Coverage target: **100% for MailVolt.Core**

### 14.2 MailVolt.Transport.Tests

Write xUnit tests using `InMemorySender` for:
- `SmtpSender` — mock `MailKitClient`, verify MimeMessage construction
- `MailgunSender` — mock `HttpClient`, verify form data fields and headers
- `SendGridSender` — mock `HttpClient`, verify JSON payload, verify inline attachments
- `ResendSender` — mock `HttpClient`, verify payload
- All senders: verify `h:Reply-To` / `reply_to` is set from EmailMessage.ReplyTo
- All senders: verify inline images (ContentId) are mapped correctly
- All senders: verify custom headers are passed through
- All senders: verify tags are passed through

### 14.3 MailVolt.Templates.Tests

- `LiquidTemplateRenderer` — renders template with model, handles missing variables gracefully
- `HandlebarsTemplateRenderer` — renders template, caches compiled template
- `RazorTemplateRenderer` — integration test with real Razor engine

### 14.4 MailVolt.Integration.Tests

These tests run only in CI with real credentials (via GitHub Secrets + Mailtrap):
- SMTP send via MailKit to Mailtrap
- Verify email arrives in Mailtrap inbox via Mailtrap API
- Mark integration tests with `[Trait("Category", "Integration")]`
- Skip in unit test runs via `dotnet test --filter "Category!=Integration"`

---

## Phase 15 — Samples

### Sample.AspNetCore

ASP.NET Core Web API (**net10.0**) demonstrating:
- Registration with SMTP and Razor templates
- Controller that sends welcome email
- Inline image in email
- `appsettings.json` configuration
- Uses C# 14 extension members for custom builder helpers

### Sample.WorkerService

Background worker demonstrating:
- Batch email sending with `IBatchEmailSender`
- Rate limiting and concurrency
- Cancellation handling

### Sample.MinimalApi

Minimal API demonstrating:
- Registration with Resend transport
- Liquid template usage
- Result handling

---

## Phase 16 — Documentation

### 16.1 README.md (root)

Structure:
```
# MailVolt

> Modern .NET email library. Drop-in replacement for FluentEmail.

## Why MailVolt?
[table: FluentEmail problems → MailVolt solutions]

## Quick Start
[code: 5 lines from nuget install to sent email]

## Installation
[all package names with dotnet add package commands]

## Senders
[table: provider → package → link to docs]

## Templates
[table: engine → package → link to docs]

## Batch Sending
[code example]

## Testing
[code example with InMemorySender]

## Contributing
[link to CONTRIBUTING.md]

## License
MIT
```

### 16.2 docs/getting-started.md

Full step-by-step from install to first email. Include:
- SMTP (most common)
- SendGrid
- Razor template
- InMemorySender in tests
- Configuration from appsettings.json

### 16.3 Sender docs (one file per sender)

Each `docs/senders/{name}.md` must include:
- NuGet install command
- Registration code
- All options with descriptions and defaults
- Full send example
- Link to provider API docs

### 16.4 XML Documentation

Every `public` type and member in `MailVolt.Core` MUST have `<summary>` XML docs.
Transport packages: document all Options properties and DI extension methods.

---

## Phase 17 — CI/CD

### 17.1 `.github/workflows/ci.yml`

Triggers: `push` to any branch, `pull_request` to `main`.

Steps:
1. `actions/checkout@v4`
2. `actions/setup-dotnet@v4` with .NET 8 and .NET 9
3. `dotnet restore`
4. `dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true`
5. `dotnet test --no-build --configuration Release --filter "Category!=Integration" --collect:"XPlat Code Coverage"`
6. Upload coverage to Codecov (optional, use `codecov/codecov-action@v4`)
7. On PR: post coverage summary as PR comment

```yaml
name: CI

on:
  push:
    branches: ['**']
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        # Test on all three supported TFMs. net10.0 is the primary — runs first.
        dotnet: ['10.x', '9.x', '8.x']
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.x
            9.x
            10.x
      - name: Restore
        run: dotnet restore
      - name: Build (TreatWarningsAsErrors)
        run: dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true
      - name: Test (unit only, targeting ${{ matrix.dotnet }})
        run: |
          dotnet test --no-build --configuration Release \
            --filter "Category!=Integration" \
            --framework net${{ matrix.dotnet == '10.x' && '10.0' || matrix.dotnet == '9.x' && '9.0' || '8.0' }} \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage
      - uses: codecov/codecov-action@v4
        if: matrix.dotnet == '10.x'   # upload coverage only from primary TFM run
        with:
          directory: ./coverage
```

### 17.2 `.github/workflows/release.yml`

Triggers: `push` to `main` with tag `v*.*.*`.

Steps:
1. Checkout with full history
2. Setup .NET 8 + 9
3. Build in Release
4. Run all non-integration tests
5. Pack all src/* projects: `dotnet pack --configuration Release --output ./artifacts`
6. Upload artifacts
7. Create GitHub Release with auto-generated release notes

```yaml
name: Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  release:
    runs-on: ubuntu-latest
    permissions:
      contents: write
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.x
            9.x
            10.x
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release --filter "Category!=Integration"
      - run: dotnet pack --no-build --configuration Release --output ./artifacts
      - uses: actions/upload-artifact@v4
        with:
          name: nuget-packages
          path: ./artifacts/*.nupkg
      - uses: softprops/action-gh-release@v2
        with:
          generate_release_notes: true
          files: ./artifacts/*
```

### 17.3 `.github/workflows/publish-nuget.yml`

Triggers: manual `workflow_dispatch` OR automatically after successful `release.yml`.

Steps:
1. Download artifacts from `release.yml`
2. `dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate`

```yaml
name: Publish to NuGet

on:
  workflow_run:
    workflows: [Release]
    types: [completed]
  workflow_dispatch:
    inputs:
      run_id:
        description: 'Run ID of the Release workflow to publish'
        required: true

jobs:
  publish:
    runs-on: ubuntu-latest
    if: ${{ github.event.workflow_run.conclusion == 'success' || github.event_name == 'workflow_dispatch' }}
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: nuget-packages
          path: ./artifacts
          github-token: ${{ secrets.GITHUB_TOKEN }}
          run-id: ${{ github.event.workflow_run.id || inputs.run_id }}
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.x   # use primary TFM for push tool
      - run: |
          dotnet nuget push ./artifacts/*.nupkg \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --source https://api.nuget.org/v3/index.json \
            --skip-duplicate
```

### 17.4 GitHub Issue Templates

**`.github/ISSUE_TEMPLATE/bug_report.md`**:
```markdown
---
name: Bug report
about: Something is broken
labels: bug
---
**Package:** MailVolt.XXX vX.X.X
**Runtime:** .NET X / OS
**Description:**
**Repro:**
```csharp
// minimal repro
```
**Expected:** 
**Actual:**
```

**`.github/ISSUE_TEMPLATE/feature_request.md`**:
```markdown
---
name: Feature request
about: New functionality
labels: enhancement
---
**Which package:** 
**Use case:**
**Proposed API:**
```csharp
// what you want to write
```
```

**`.github/PULL_REQUEST_TEMPLATE.md`**:
```markdown
## What does this PR do?

## Related issues
Closes #

## Checklist
- [ ] Tests added/updated
- [ ] XML docs updated for new public API
- [ ] `dotnet build /p:TreatWarningsAsErrors=true` passes
- [ ] `dotnet test` passes
```

---

## Phase 18 — Versioning Strategy

All packages share the SAME version. Use a single `<Version>` in `Directory.Build.props`.

Version scheme: **SemVer** (`MAJOR.MINOR.PATCH`)
- `1.0.0` — stable release
- Pre-release: `1.0.0-alpha.1`, `1.0.0-beta.1`, `1.0.0-rc.1`

To release a new version:
1. Update `<Version>` in `Directory.Build.props`
2. Commit: `chore: bump version to 1.1.0`
3. Tag: `git tag v1.1.0 && git push origin v1.1.0`
4. `release.yml` fires automatically

---

## Phase 19 — CONTRIBUTING.md

Include:
- Development setup: **.NET 10 SDK recommended** (minimum: .NET 8 SDK)
- Build: `dotnet build`
- Test: `dotnet test --filter "Category!=Integration"`
- To test a specific TFM: `dotnet test --framework net10.0 --filter "Category!=Integration"`
- Code style: enforced by `.editorconfig`, `TreatWarningsAsErrors=true`, C# 14
- Commit messages: Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:`, `test:`)
- PR process: fork → feature branch → PR to `main`
- Issue first for large features
- XML docs required for all public API
- One package per PR ideally
- Note: C# 14 features are used throughout — contributors need VS 2026 or VS Code + C# Dev Kit

---

## Implementation Order (strict)

Execute phases in this exact order. After each phase, run:
```bash
dotnet build --configuration Release /p:TreatWarningsAsErrors=true
dotnet test --filter "Category!=Integration"
```

Both MUST pass before proceeding to the next phase.

```
Phase 0  → Bootstrap (repo structure, build props, license)
Phase 1  → MailVolt.Core (domain, interfaces, builder, DI)
Phase 14.1 → Core unit tests (write tests for Core before transports)
Phase 2  → MailVolt.Transport.Smtp
Phase 3  → MailVolt.Transport.Mailgun
Phase 4  → MailVolt.Transport.SendGrid
Phase 5  → MailVolt.Transport.Resend
Phase 6  → MailVolt.Transport.Postmark
Phase 7  → MailVolt.Transport.AzureEmail
Phase 8  → MailVolt.Transport.Brevo
Phase 9  → MailVolt.Transport.AwsSes
Phase 13 → MailVolt.Testing
Phase 14.2 → Transport tests
Phase 10 → MailVolt.Templates.Razor
Phase 11 → MailVolt.Templates.Liquid
Phase 12 → MailVolt.Templates.Handlebars
Phase 14.3 → Template tests
Phase 15 → Samples
Phase 16 → Documentation (README + docs/ folder)
Phase 17 → CI/CD workflows
Phase 18 → Versioning
Phase 19 → CONTRIBUTING.md
```

---

## Done Checklist

Mark each item when complete:

### Phase 0
- [ ] `Directory.Build.props` created with all properties
- [ ] `Directory.Packages.props` created with all versions
- [ ] `.editorconfig` created
- [ ] `LICENSE` (MIT) created
- [ ] `CODE_OF_CONDUCT.md` created
- [ ] `SECURITY.md` created
- [ ] `.gitignore` created
- [ ] `MailVolt.sln` created with all projects

### Phase 1 — Core
- [ ] `EmailAddress.cs` with implicit string conversion
- [ ] `EmailAttachment.cs` with IsInline property
- [ ] `EmailPriority.cs` enum
- [ ] `EmailMessage.cs` fully immutable
- [ ] `EmailResult.cs` with Success/Failure factories
- [ ] `BatchEmailResult.cs`
- [ ] `ISender.cs` interface
- [ ] `ITemplateRenderer.cs` interface
- [ ] `IEmailBuilder.cs` interface
- [ ] `IAttachmentBuilder.cs` interface
- [ ] `IBatchEmailSender.cs` interface + BatchSendOptions + BatchFailureStrategy
- [ ] `MailVoltOptions.cs`
- [ ] `EmailBuilder.cs` full implementation
- [ ] `AttachmentBuilder.cs` with MIME type detection dict
- [ ] `BatchEmailSender.cs` with SemaphoreSlim concurrency
- [ ] `MailVoltServiceCollectionExtensions.cs` + `MailVoltBuilder`
- [ ] Core.csproj with correct metadata

### Phase 2 — SMTP
- [ ] `SmtpSenderOptions.cs`
- [ ] `SmtpSender.cs` using MailKit, new client per call
- [ ] Inline images via `builder.LinkedResources`
- [ ] OAuth2 support
- [ ] `SmtpTransportExtensions.cs`

### Phase 3 — Mailgun
- [ ] `MailgunSenderOptions.cs`
- [ ] `MailgunSender.cs` using IHttpClientFactory
- [ ] `h:Reply-To` mapping
- [ ] Inline attachments as `inline` multipart
- [ ] Tags as `o:tag`
- [ ] Custom headers as `h:{name}`
- [ ] `MailgunTransportExtensions.cs` with `.AddStandardResilienceHandler()`

### Phase 4 — SendGrid
- [ ] `SendGridSenderOptions.cs`
- [ ] `SendGridSender.cs` using System.Text.Json
- [ ] Inline attachments (disposition: inline, content_id)
- [ ] Custom host support
- [ ] Dynamic templates support

### Phase 5 — Resend
- [ ] `ResendSenderOptions.cs`
- [ ] `ResendSender.cs`
- [ ] Batch API support

### Phase 6 — Postmark
- [ ] `PostmarkSenderOptions.cs`
- [ ] `PostmarkSender.cs`
- [ ] MessageStream support

### Phase 7 — Azure
- [ ] `AzureEmailSenderOptions.cs`
- [ ] `AzureEmailSender.cs`

### Phase 8 — Brevo
- [ ] `BrevoSenderOptions.cs`
- [ ] `BrevoSender.cs`

### Phase 9 — AWS SES
- [ ] `AwsSesSenderOptions.cs`
- [ ] `AwsSesSender.cs` (simple + raw MIME paths)

### Phase 10 — Razor
- [ ] `RazorTemplateOptions.cs`
- [ ] `RazorTemplateRenderer.cs` using IRazorViewEngine
- [ ] Embedded resource support
- [ ] Layout page support

### Phase 11 — Liquid
- [ ] `LiquidTemplateRenderer.cs`

### Phase 12 — Handlebars
- [ ] `HandlebarsTemplateRenderer.cs` with ConcurrentDictionary cache

### Phase 13 — Testing
- [ ] `InMemorySender.cs`
- [ ] `FailingSender.cs`
- [ ] `SentEmailAssertions.cs` (FluentAssertions extensions)
- [ ] `TestingExtensions.cs`

### Phase 14 — Tests
- [ ] Core.Tests: 100% coverage of Phase 1 code
- [ ] Transport.Tests: all senders mocked
- [ ] Templates.Tests: Liquid + Handlebars
- [ ] All tests pass: `dotnet test --filter "Category!=Integration"`

### Phase 15 — Samples
- [ ] Sample.AspNetCore compiles and runs
- [ ] Sample.WorkerService compiles and runs
- [ ] Sample.MinimalApi compiles and runs

### Phase 16 — Docs
- [ ] README.md with Why/QuickStart/Installation/Examples
- [ ] docs/getting-started.md
- [ ] docs/senders/*.md (one per sender)
- [ ] docs/templates/*.md (one per engine)
- [ ] docs/advanced/batch-sending.md
- [ ] docs/advanced/resilience.md
- [ ] docs/advanced/testing.md

### Phase 17 — CI/CD
- [ ] `.github/workflows/ci.yml`
- [ ] `.github/workflows/release.yml`
- [ ] `.github/workflows/publish-nuget.yml`
- [ ] `.github/ISSUE_TEMPLATE/bug_report.md`
- [ ] `.github/ISSUE_TEMPLATE/feature_request.md`
- [ ] `.github/PULL_REQUEST_TEMPLATE.md`

### Phase 18 — Versioning
- [ ] `<Version>1.0.0-alpha.1</Version>` in Directory.Build.props

### Phase 19 — Contributing
- [ ] `CONTRIBUTING.md`

---

## Final Verification

Before marking DONE, run all of these and verify they pass:

```bash
# Full build — all three TFMs, warnings as errors
dotnet build --configuration Release /p:TreatWarningsAsErrors=true

# Unit tests on the primary TFM (net10.0)
dotnet test --configuration Release --framework net10.0 --filter "Category!=Integration"

# Unit tests on all TFMs (net8, net9, net10)
dotnet test --configuration Release --filter "Category!=Integration"

# All packages produce valid .nupkg with all three TFMs inside
dotnet pack --configuration Release --output ./artifacts
ls ./artifacts/*.nupkg | wc -l
# Should output: 13 (one per src/* project)

# Verify each .nupkg targets net8.0, net9.0, net10.0
for pkg in ./artifacts/*.nupkg; do
  echo "=== $pkg ==="
  unzip -l "$pkg" | grep -E "lib/(net8|net9|net10)"
done

# Verify no package depends on System.Net.Mail.SmtpClient
grep -r "System.Net.Mail" src/ --include="*.cs"
# Should output: nothing

# Verify no sync Send methods exist  
grep -r "EmailResult Send(" src/ --include="*.cs"
# Should output: nothing

# Verify no GetAwaiter().GetResult()
grep -r "GetAwaiter().GetResult()" src/ --include="*.cs"
# Should output: nothing

# Verify no direct 'new HttpClient' (must use IHttpClientFactory)
grep -r "new HttpClient()" src/ --include="*.cs"
# Should output: nothing

# Verify C# 14 LangVersion is set (not 'latest')
grep "LangVersion" Directory.Build.props
# Should output: <LangVersion>14</LangVersion>
```

If all verification commands pass and the Done Checklist is 100% checked — the project is ready for v1.0.0-alpha.1 publication.
