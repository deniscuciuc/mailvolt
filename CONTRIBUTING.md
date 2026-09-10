# Contributing to MailVolt

Thank you for your interest in MailVolt! This document provides guidelines for setting up, building, testing, and submitting changes.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Local Setup](#local-setup)
- [Building](#building)
- [Testing](#testing)
- [Code Style](#code-style)
- [Adding a New Transport](#adding-a-new-transport)
- [Releasing](#releasing)
- [Pull Request Guidelines](#pull-request-guidelines)

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (primary target)
- .NET 9.0 and .NET 8.0 SDKs (for multi-target validation)
- An editor that supports `.editorconfig` (Visual Studio, Rider, VS Code with C# Dev Kit)

## Local Setup

```bash
# Clone the repository
git clone https://github.com/deniscuciuc/mailvolt.git
cd mailvolt

# Restore dependencies
dotnet restore
```

## Building

Build all projects in Release mode with warnings treated as errors:

```bash
dotnet build --configuration Release /p:TreatWarningsAsErrors=true
```

For a quick debug build:

```bash
dotnet build
```

## Testing

Run all unit tests across all target frameworks:

```bash
dotnet test --configuration Release
```

Run tests for a specific framework:

```bash
dotnet test --framework net10.0
```

Exclude integration tests (which may require external credentials):

```bash
dotnet test --filter "Category!=Integration"
```

> **Note:** Tests tagged with `[Trait("Category", "Integration")]` require real service credentials and are excluded from CI. Run them locally only when working on transport integrations.

Run tests with code coverage:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

## Code Style

MailVolt follows a strict, consistent code style enforced by the build:

- **Language:** C# 14 (configured via `Directory.Build.props`)
- **Nullable reference types:** Enabled project-wide (`<Nullable>enable</Nullable>`)
- **Implicit usings:** Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Async-only:** All I/O-bound public APIs must be async (`Task`/`ValueTask`-returning). Avoid `sync over async` patterns.
- **Warnings as errors:** All builds use `TreatWarningsAsErrors=true`. Zero warnings in production code.
- **Documentation:** All public types and members must have XML doc comments (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
- **Formatting:** Follow `.editorconfig` conventions (4-space indentation, LF line endings, UTF-8).
- **General:**

  - Prefer `primary constructors` for simple DI scenarios
  - Use `file-scoped namespace` declarations
  - Use `readonly` fields wherever possible
  - Favor `ImmutableArray<T>`/`IReadOnlyList<T>` over mutable collections in public signatures
  - Use `ArgumentNullException.ThrowIfNull()` for null guards
  - No `Region` blocks

## Adding a New Transport

Transports are the pluggable email providers in MailVolt. To add a new one:

1. **Create the project:**

   ```bash
   dotnet new classlib -o src/MailVolt.Transport.YourProvider -n MailVolt.Transport.YourProvider
   ```

2. **Add to the solution:** edit `MailVolt.slnx` and add the project under
   `Folder Name="/src/"`. Add a `README.md` next to the `.csproj` — each package ships its own.

3. **Implement `ISender`:**

   ```csharp
   using MailVolt.Core.Interfaces;
   using MailVolt.Core.Models;
   using Microsoft.Extensions.Options;

   namespace MailVolt.Transport.YourProvider;

   public sealed class YourProviderSender(IOptions<YourProviderSenderOptions> options) : ISender
   {
       private readonly YourProviderSenderOptions _options = options.Value;

       public async Task<EmailResult> SendAsync(
           EmailMessage email,
           CancellationToken cancellationToken = default)
       {
           ArgumentNullException.ThrowIfNull(email);

           try
           {
               var request = MapToProviderRequest(email);
               // ... send ...
               return EmailResult.Success(messageId);
           }
           // Cancellation must propagate, not become a failed result — every transport
           // behaves the same way so swapping providers changes nothing observable.
           catch (Exception ex) when (ex is not OperationCanceledException)
           {
               return EmailResult.Failure(ex.Message, ex);
           }
       }

       // Internal rather than private, so the mapping is unit testable without calling
       // the provider. Every existing transport exposes this seam.
       internal static ProviderRequest MapToProviderRequest(EmailMessage email) { /* ... */ }
   }
   ```

   Requirements the existing transports all meet, and that tests check:

   - `ConfigureAwait(false)` on every `await` (CA2007 enforces this).
   - Map `From`, `To`, `Cc`, `Bcc`, **`ReplyTo`**, `Subject`, both bodies, `Headers`, `Tags`
     and `Priority`.
   - Handle `EmailAttachment.IsInline` as the provider's inline mechanism, not as an ordinary
     attachment, so a `cid:` reference resolves.
   - Do not log or return raw provider response bodies containing credentials.

4. **Add an options class** with a `SectionName` constant of `"MailVolt:YourProvider"`.

5. **Add the DI extension** in `MailVolt.Core.DependencyInjection` — all registration methods
   share that namespace so one `using` covers everything — named
   `UseYourProviderTransport`:

   ```csharp
   using MailVolt.Core.Interfaces;
   using MailVolt.Transport.YourProvider;
   using Microsoft.Extensions.Configuration;
   using Microsoft.Extensions.DependencyInjection;

   // ReSharper disable once CheckNamespace
   namespace MailVolt.Core.DependencyInjection;

   public static class YourProviderTransportExtensions
   {
       public static MailVoltBuilder UseYourProviderTransport(
           this MailVoltBuilder builder,
           Action<YourProviderSenderOptions> configure)
       {
           ArgumentNullException.ThrowIfNull(builder);
           ArgumentNullException.ThrowIfNull(configure);

           builder.Services.Configure(configure);
           builder.Services.AddTransient<ISender, YourProviderSender>();
           return builder;
       }

       public static MailVoltBuilder UseYourProviderTransport(
           this MailVoltBuilder builder,
           IConfiguration configuration)
       {
           ArgumentNullException.ThrowIfNull(builder);
           ArgumentNullException.ThrowIfNull(configuration);

           // Takes the configuration root and resolves its own section, so the section name
           // lives in exactly one place. Do not accept a pre-scoped IConfigurationSection.
           builder.Services.Configure<YourProviderSenderOptions>(
               configuration.GetSection(YourProviderSenderOptions.SectionName));
           builder.Services.AddTransient<ISender, YourProviderSender>();
           return builder;
       }
   }
   ```

   If the transport owns an `HttpClient`, register it with
   `AddHttpClient<...>().AddStandardResilienceHandler()`.

6. **Add tests** in `tests/MailVolt.Transport.Tests/`: mapping tests against the `internal
   static` seam, DI-registration tests in `TransportDiTests`, a case in
   `ConfigurationBindingTests`, and an entry in `CancellationSemanticsTests`.

7. **Wire it into AutoConfigure:** add a `MailVoltTransport` enum value and a `WireTransport`
   case, then add it to the theory data in `tests/MailVolt.AutoConfigure.Tests/AllTransportsTests.cs`.

8. **Document it:** add `docs/senders/yourprovider.md`, and add rows to the tables in
   `README.md` and `docs/README.md`. Any code you put in the docs must also go in
   `tests/MailVolt.Docs.Snippets`, which compiles every documented snippet in CI.

## Releasing

Releases are automated via GitHub Actions. See [`docs/release.md`](docs/release.md) for the full release process, including:

- Versioning conventions (e.g. `0.1.0-preview.1` for first preview releases)
- How to create a release by pushing a `v*.*.*` tag
- NuGet Trusted Publishing setup (no long-lived API key required)

Only maintainers can publish releases.

## Pull Request Guidelines

1. **Scope:** Keep PRs focused on a single concern. Split large changes into multiple PRs.

2. **Tests:** All new features and bug fixes must include tests. Verify existing tests still pass.

3. **Breaking changes:** Annotate with `[Obsolete]` before removal. Discuss breaking changes in an issue first.

4. **Commit messages:** Use conventional commits format:
   ```
   feat(core): add support for attachment streaming
   fix(smtp): handle connection timeout gracefully
   docs(readme): add Azure Email Transport to provider list
   ```

5. **Before submitting:**
   - Rebase onto the latest `main`
   - Run the full build + test suite
   - Ensure zero new warnings or analyzer violations

6. **Review:** All PRs require at least one maintainer review. Address review feedback with additional commits (no force-push during review).

Thank you for contributing!
