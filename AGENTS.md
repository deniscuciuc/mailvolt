# MailVolt — Agent Guide

This document captures the project structure, conventions, and workflows that AI coding agents should follow when working in the MailVolt repository.

## Project Overview

MailVolt is a modern, async-first, DI-first .NET email library positioned as a maintained alternative to FluentEmail. It provides a fluent `IEmailBuilder`, pluggable transports (SMTP, SendGrid, Mailgun, Resend, Postmark, Azure Email, Brevo, AWS SES), template rendering (Razor, Liquid, Handlebars), batch sending with concurrency control, and first-class test helpers.

Key architectural traits:

- Async-only public API (`Task`/`ValueTask`-returning).
- Dependency-injection first; configuration is done through `IServiceCollection` extensions and a `MailVoltBuilder`.
- Multi-targeting: `net8.0`, `net9.0`, `net10.0`.
- AOT/trimming annotations enabled only for `MailVolt.Core` on `net10.0`.
- All production builds treat warnings as errors and require XML documentation on public members.

## Repository Layout

```
mailvolt/
├── src/                            # Packable library projects
│   ├── MailVolt.Core/              # Abstractions, models, fluent builder, batch sender, DI extensions
│   ├── MailVolt.AutoConfigure/     # Zero-code configuration from IConfiguration
│   ├── MailVolt.Testing/           # InMemorySender, FailingSender, FluentAssertions extensions
│   ├── MailVolt.Transport.*        # One project per provider (Smtp, SendGrid, Mailgun, Resend, Postmark, AzureEmail, Brevo, AwsSes)
│   └── MailVolt.Templates.*        # Razor, Liquid, Handlebars renderers
├── tests/                          # Unit / integration test projects
│   ├── MailVolt.Core.Tests/
│   ├── MailVolt.Transport.Tests/
│   ├── MailVolt.Templates.Tests/
│   ├── MailVolt.AutoConfigure.Tests/
│   └── MailVolt.Integration.Tests/ # Currently empty placeholder
├── examples/                       # Standalone runnable samples
│   ├── 01-console-smtp-razor/
│   ├── 02-console-sendgrid-liquid/
│   ├── ...
│   └── 09-testing-demo/
├── docs/                           # Markdown documentation
├── .github/workflows/              # CI, release, and NuGet publish pipelines
├── Directory.Build.props           # Shared MSBuild properties for all projects
├── Directory.Packages.props        # Central package management (CPM)
└── MailVolt.slnx                   # Solution file (XML format)
```

## Technology Stack

- **SDKs:** .NET 8.0, .NET 9.0, .NET 10.0 SDKs. Primary development target is .NET 10.0.
- **Language:** C# 14 (`LangVersion` pinned in `Directory.Build.props`).
- **Nullable reference types** and **implicit usings** enabled globally.
- **Build system:** MSBuild / `dotnet` CLI, Central Package Management via `Directory.Packages.props`.
- **Core dependencies:**
  - `Microsoft.Extensions.*` (DI, Options, Configuration, Logging, Http, Hosting) v10.0.0
  - `MailKit` / `MimeKit` 4.17.0 (SMTP transport)
  - `Microsoft.Extensions.Http.Resilience` 9.0.0 (HTTP transports)
  - Provider SDKs: `Azure.Communication.Email`, `AWSSDK.SimpleEmailV2`, `brevo_csharp`, `Postmark`, `Resend`, `SendGrid`, `SendGrid.Extensions.DependencyInjection`
- **Template engines:** `Fluid.Core` (Liquid), `Handlebars.Net`, native ASP.NET Core Razor (`Microsoft.AspNetCore.App` framework reference).
- **Testing:** xUnit 2.9.3, NSubstitute 5.3.0, FluentAssertions 8.2.0, coverlet.collector 6.0.4.
- **Source linking:** `Microsoft.SourceLink.GitHub` is referenced in all packable projects.

## Build, Test, and Pack Commands

All commands run from the repository root.

```bash
# Restore dependencies
dotnet restore

# Debug build
dotnet build

# Release build with warnings-as-errors (what CI does)
dotnet build --configuration Release /p:TreatWarningsAsErrors=true

# Run all tests across all target frameworks
dotnet test --configuration Release

# Run tests for a single target framework
dotnet test --framework net10.0

# Exclude integration tests (they require live credentials)
dotnet test --filter "Category!=Integration"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Pack NuGet packages (typically done by the release workflow)
dotnet pack --configuration Release --output ./artifacts
```

The CI pipeline (`.github/workflows/ci.yml`) builds and tests on `ubuntu-latest` against .NET 8.x, 9.x, and 10.x, excluding integration tests and uploading coverage to Codecov for the .NET 10.x matrix leg.

Release and publish are handled by separate workflows:

- `.github/workflows/release.yml` triggers on `v*.*.*` tags, builds, packs, creates a GitHub release, and publishes the packages to NuGet.org using **Trusted Publishing** (OIDC). No long-lived `NUGET_API_KEY` secret is required; the workflow exchanges a short-lived GitHub OIDC token for a temporary NuGet API key via `NuGet/login@v1`.

## Code Organization

### Core abstractions (`MailVolt.Core`)

- `Interfaces/ISender.cs` — the single-message transport contract.
- `Interfaces/IEmailBuilder.cs` — the fluent builder contract.
- `Interfaces/ITemplateRenderer.cs` — template rendering contract.
- `Interfaces/IBatchEmailSender.cs` — batch sending contract plus `BatchSendOptions` and `FailureStrategy`.
- `Models/EmailMessage.cs` — immutable record representing a constructed email.
- `Models/EmailResult.cs` — success/failure result record.
- `Models/EmailAddress.cs`, `EmailAttachment.cs`, `EmailPriority.cs`, `BatchEmailResult.cs`
- `EmailBuilder.cs` — internal implementation of `IEmailBuilder`.
- `BatchEmailSender.cs` — internal implementation using `SemaphoreSlim` for concurrency.
- `DependencyInjection/MailVoltServiceCollectionExtensions.cs` — registers `IEmailBuilder` and `IBatchEmailSender` and returns a `MailVoltBuilder`.

`MailVolt.Core` is intentionally small and has no transport-specific dependencies. Transports and template engines are separate packages that register implementations of `ISender` and `ITemplateRenderer`.

### Transports (`MailVolt.Transport.*`)

Each transport project:

- Implements `ISender` (often named `*Sender`).
- Defines its own options class (e.g., `SmtpSenderOptions`).
- Provides DI extension methods on `MailVoltBuilder` (e.g., `UseSmtpTransport`, `AddSendGridSender`).
- HTTP-based transports use typed `HttpClient` instances registered with `AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience`.

### Templates (`MailVolt.Templates.*`)

Each template package implements `ITemplateRenderer` and exposes a `Use*Templates` extension on `MailVoltBuilder`. `MailVolt.Templates.Razor` requires the `Microsoft.AspNetCore.App` framework reference because it uses native ASP.NET Core Razor.

### AutoConfigure (`MailVolt.AutoConfigure`)

A convenience meta-package that references Core, every transport, every template engine, and Testing. It adds:

```csharp
services.AddMailVolt(configuration, sectionName = "MailVolt");
```

This reads the `MailVolt` configuration section, binds `MailVoltAutoOptions`, and wires the selected transport and optional template engine. Supported transports: `Smtp`, `SendGrid`, `Mailgun`, `Resend`, `Postmark`, `Azure`, `Brevo`, `AwsSes`, `InMemory`. Supported templates: `Razor`, `Liquid`, `Handlebars`.

### Testing (`MailVolt.Testing`)

- `InMemorySender` — singleton-captured `ISender` for assertions.
- `FailingSender` — deterministic failure generator.
- `InMemorySenderAssertions` — custom FluentAssertions extension methods (`HaveCount`, `ContainEmailTo`, `ContainSubject`, etc.).
- `DependencyInjection/TestingExtensions.cs` — `UseInMemoryTransport()`.

## Code Style Guidelines

Style is enforced by `.editorconfig` and by MSBuild (`TreatWarningsAsErrors=true`).

- **C# version:** 14.
- **Nullable reference types** enabled; use `ArgumentNullException.ThrowIfNull(...)` for guards.
- **Implicit usings** enabled; avoid redundant `using` directives.
- **Namespaces:** file-scoped (`namespace Foo.Bar;`).
  - Note: `.editorconfig` currently sets `csharp_style_namespace_declarations = block_scoped`, but the existing codebase uses file-scoped namespaces. Follow the existing code.
- **Formatting:** 4-space indentation for `.cs` files, 2-space for `.csproj`/`.props`/`.targets`, LF line endings, UTF-8, trim trailing whitespace, final newline.
- **Constructors:** prefer primary constructors for simple DI scenarios.
- **Collections in public APIs:** favor `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey,TValue>`, or `ImmutableArray<T>` over mutable types.
- **Readonly:** use `readonly` fields where possible.
- **Regions:** do not use `#region` blocks.
- **Documentation:** all public types and members must have XML doc comments; `GenerateDocumentationFile` is true. Packable projects suppress `CS1591` to avoid missing-comment warnings on `Program` classes in examples.
- **Async:** all I/O-bound public APIs must be async. Do not introduce sync-over-async.
- **Warnings:** zero warnings in production code. Specific suppressed warnings are configured in `Directory.Build.props` (`NU1902`, `NU1903`, `NU1506`, `IL2026`, `IL3050`).

## Testing Strategy

- **Framework:** xUnit with FluentAssertions and NSubstitute.
- **Test projects:**
  - `MailVolt.Core.Tests` — builder validation, batch sender behavior, models, DI registration.
  - `MailVolt.Transport.Tests` — DI registration, request/response mapping, attachment handling for each transport using stubbed `HttpMessageHandler` where applicable.
  - `MailVolt.Templates.Tests` — rendering output for Razor, Liquid, and Handlebars.
  - `MailVolt.AutoConfigure.Tests` — configuration binding and wiring logic.
  - `MailVolt.Integration.Tests` — placeholder project for live-service tests requiring credentials.
- **Integration tests:** tests that require real provider credentials must be decorated with `[Trait("Category", "Integration")]`. They are excluded from CI and normal local runs via `--filter "Category!=Integration"`.
- **Coverage:** CI collects coverage with coverlet and uploads to Codecov.
- **Test helpers:** use `InMemorySender` and `FailingSender` from `MailVolt.Testing`; use the `Helpers` static class in `MailVolt.Transport.Tests` for common stubs and test email factories.

When adding a new feature or bug fix, include unit tests. Do not change existing test logic unless the underlying contract changes.

## Security Considerations

- **Credentials:** provider API keys, SMTP passwords, and connection strings must only live in user secrets, environment variables, or uncommitted `appsettings.json` files. `.gitignore` excludes `**/appsettings.json` and `examples/**/appsettings.json`. Examples ship with `appsettings.example.json` templates.
- **Environment variable separator:** configuration uses the standard .NET `__` separator, e.g. `MailVolt__SendGrid__ApiKey`.
- **Do not commit secrets.** The build is deterministic and publishes symbol packages; SourceLink embeds repository metadata but not credentials.
- **Vulnerability reports:** see `SECURITY.md`; contact `denis.cuciuc@zelqonworks.com`.
- **Transitive vulnerabilities:** `Directory.Build.props` suppresses `NU1902`/`NU1903` for transitive dependency warnings; do not use this as an excuse to ignore security updates in direct dependencies.

## Adding a New Transport

1. Create a new class library at `src/MailVolt.Transport.YourProvider`.
2. Add the project to `/src/` in `MailVolt.slnx`.
3. Implement `ISender`.
4. Add an options class and DI extension methods on `MailVoltBuilder`.
5. For HTTP providers, register a typed `HttpClient` and `AddStandardResilienceHandler()`.
6. Add unit tests in `tests/MailVolt.Transport.Tests/`.
7. Add the transport to `MailVolt.AutoConfigure` wiring and `docs/autoconfigure.md`.
8. Update `README.md` provider table.

## Useful References

- `README.md` — user-facing quick start and package list.
- `CONTRIBUTING.md` — contributor setup, style, and PR guidelines.
- `docs/autoconfigure.md` — full `appsettings.json` schema.
- `docs/advanced/testing.md` — testing utilities and patterns.
- `docs/advanced/batch-sending.md` — batch API details.
- `docs/advanced/resilience.md` — HTTP resilience pipeline details.
