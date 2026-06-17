# MailVolt Copilot Instructions

## Build, test, and lint

- Restore dependencies: `dotnet restore`
- Fast local build: `dotnet build`
- CI/release build: `dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true`
- Full test suite across target frameworks: `dotnet test --configuration Release`
- CI-style unit test run for one target framework: `dotnet test --no-build --configuration Release --framework net10.0 --filter "Category!=Integration"`
- Run a single test: `dotnet test tests/MailVolt.Core.Tests/MailVolt.Core.Tests.csproj --filter "FullyQualifiedName~MailVolt.Core.Tests.EmailBuilderTests.BuildAsync_applies_default_from_address"`
- Collect coverage: `dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage`
- There is no separate lint command in the repo today; the warnings-as-errors build and `.editorconfig` are the enforcement gate.

## High-level architecture

- `src/MailVolt.Core` is the composition root for the library API. `AddMailVolt()` registers `IEmailBuilder` and `IBatchEmailSender`; `EmailBuilder` accumulates an immutable `EmailMessage`, optionally renders a template through `ITemplateRenderer`, and delegates sending to `ISender`. `BatchEmailSender` reuses the same `ISender` abstraction for concurrent batch delivery with `BatchSendOptions`.
- Each `src/MailVolt.Transport.*` project is a pluggable provider package. HTTP-backed providers typically register a typed `HttpClient` plus `AddStandardResilienceHandler()` and then expose that implementation as the active `ISender`; SMTP, AWS SES, and Azure Email register concrete senders directly.
- Template support is split into `MailVolt.Templates.Razor`, `MailVolt.Templates.Liquid`, and `MailVolt.Templates.Handlebars`, each registering only `ITemplateRenderer`. `MailVolt.AutoConfigure` is the config-first layer that binds the `MailVolt` section and then calls the same transport/template registration methods used by manual DI setup.
- `MailVolt.Testing` is the standard test seam. `UseInMemoryTransport()` swaps in a singleton `InMemorySender` with custom assertion helpers, and the test projects under `tests/` mirror the package split in `src/`. `examples/` projects are included in the solution and act as executable reference apps.

## Key conventions

- Treat `ISender` as the single transport seam. New providers should register themselves as `ISender`, and if they expose a provider-specific interface (`IMailgunSender`, `IResendSender`, etc.), that same implementation should also be aliased back to `ISender`.
- Keep manual DI wiring and auto-configuration wiring in sync. If a transport or template package changes its registration shape, update both its extension methods and the switch logic in `MailVoltAutoConfigureExtensions`.
- Public library projects are multi-targeted (`net8.0;net9.0;net10.0`) with C# 14, nullable enabled, XML docs generated, and warnings treated as errors via `Directory.Build.props`. Test and example projects explicitly relax some of those rules instead of changing the shared defaults.
- The codebase leans on the fluent builder API and immutable models (`EmailMessage`, `EmailAddress`, `BatchSendOptions`) rather than mutable service objects. Call sites commonly rely on the implicit `string -> EmailAddress` conversion, so avoid rewriting examples/tests to construct `EmailAddress` manually unless a display name is needed.
- Configuration-first flows fail fast. `MailVolt.AutoConfigure` throws for missing sections or required values instead of silently falling back, so preserve explicit validation when extending config binding.
- Prefer `UseInMemoryTransport()` for unit tests. It is intentionally registered as a singleton so the same `InMemorySender` instance can be inspected after exercising the code under test.
