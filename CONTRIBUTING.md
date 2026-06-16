# Contributing to MailVolt

Thank you for your interest in MailVolt! This document provides guidelines for setting up, building, testing, and submitting changes.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Local Setup](#local-setup)
- [Building](#building)
- [Testing](#testing)
- [Code Style](#code-style)
- [Adding a New Transport](#adding-a-new-transport)
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

2. **Add to solution:** Edit `MailVolt.slnx` and add the project under `Folder Name="/src/"`.

3. **Inherit from `IEmailTransport`:**

   ```csharp
   using MailVolt.Core.Abstractions;

   public sealed class YourProviderTransport(IOptions<YourProviderOptions> options)
       : IEmailTransport
   {
       public async Task<SendEmailResult> SendAsync(
           Email email,
           CancellationToken cancellationToken = default)
       {
           // Your implementation
       }
   }
   ```

4. **Create extension method for DI registration:**

   ```csharp
   public static class MailVoltBuilderExtensions
   {
       public static MailVoltBuilder UseYourProvider(
           this MailVoltBuilder builder,
           Action<YourProviderOptions> configure)
       {
           builder.Services.Configure(configure);
           builder.Services.AddSingleton<IEmailTransport, YourProviderTransport>();
           return builder;
       }
   }
   ```

5. **Add tests:** Create unit tests in `tests/MailVolt.Transport.Tests/` that mock the HTTP handler.

6. **Update documentation:** List the new transport in the README table.

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
