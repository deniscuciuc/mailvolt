# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.1] - 2026-09-10

### Changed

- New package icon. The previous one had opaque black corners, which nuget.org renders on a
  light background — visible on every package page. The new mark integrates the lightning
  bolt into the envelope's flap crease so it reads as one silhouette at the 32px size
  nuget.org uses in search results, and its corners are transparent.
- Added a banner to the root README and to all 14 per-package READMEs.

## [0.1.0] - 2026-09-10

First stable release. The API is settled; changes from `0.1.0-preview.3` are listed below.

### Breaking

- `EmailAttachment.Content` is now `ReadOnlyMemory<byte>` rather than `Stream`. An
  `EmailMessage` is therefore immutable and can be sent more than once — previously the
  first send consumed the stream, so a retry silently sent an empty attachment.
  `EmailAttachment.OpenReadStream()` covers callers that still need a stream.
- `InMemorySender`, `FailingSender` and `SentEmail` moved from `MailVolt.Testing` to
  `MailVolt.Core.Transports`, and `UseInMemoryTransport` from
  `MailVolt.Testing.DependencyInjection` to `MailVolt.Core.DependencyInjection`.
  `MailVolt.AutoConfigure` no longer references `MailVolt.Testing`, so installing it no
  longer pulls an assertion library into a production dependency graph.
- All transport and template registration extensions moved to
  `MailVolt.Core.DependencyInjection`. One `using` now covers `AddMailVolt` and every
  transport and template engine.
- Five registration methods renamed for consistency: `AddSendGridSender` →
  `UseSendGridTransport`, `AddBrevoSender` → `UseBrevoTransport`, `AddAzureEmailSender` →
  `UseAzureEmailTransport`, `AddPostmarkSender` → `UsePostmarkTransport`, `UseResend` →
  `UseResendTransport`.
- `UseResendTransport(IConfiguration)` takes the configuration root instead of a pre-scoped
  `IConfigurationSection`, matching the other seven transports.
- A cancelled send now raises `OperationCanceledException` on every transport. SMTP, AWS SES
  and Azure previously returned a failed `EmailResult`, and Postmark returned a failure whose
  message said "cancelled".
- `UsingTemplate` throws when no `ITemplateRenderer` is registered or the model is `null`,
  instead of silently sending an email with an empty body.
- `EmailAddress` validates its input and throws `ArgumentException` for a malformed address.
  Use `EmailAddress.TryParse` or `EmailAddress.IsValid` to check without catching.
- `IEmailBuilder` methods validate their arguments and throw on `null`.

### Added

- `BatchEmailResult.SkippedCount`, so `SentCount`, `FailedCount` and `SkippedCount` always sum
  to `TotalCount` when `StopOnFirstFailure` halts a batch.
- Handlebars helper and partial registration via
  `UseHandlebarsTemplates(options => options.RegisterHelper(...).RegisterPartial(...))`.
  The renderer uses its own Handlebars environment, so registrations cannot leak into another
  library in the same process.
- `UseFailingTransport`, a registration helper for `FailingSender`.
- `IAttachmentBuilder.FromMemory`, and `EmailAttachment.OpenReadStream`.
- Resilience handlers for the Resend and SendGrid transports, so every transport that owns an
  `HttpClient` has retries, a circuit breaker and timeouts.
- `RazorTemplateOptions.RootDirectory` is now honoured; it was configurable but never read.
- Documentation: a FluentEmail migration guide, a troubleshooting guide, an attachments and
  inline-images page, and a docs index.
- `.NET analyzers`, `NuGetAudit`, `global.json`, `.gitattributes` and `CODEOWNERS`.

### Fixed

- `AttachmentBuilder.FromFile` opened a `FileStream` that nothing disposed, leaking a file
  handle per attached email. File and stream reads now happen asynchronously during
  `BuildAsync`.
- `EmailBuilder` returned a live view over its internal recipient list rather than a copy, so
  building a second message from the same builder changed the first one's recipients.
- `BatchEmailSender` never disposed its `SemaphoreSlim`, and did not validate
  `MaxConcurrency` or `DelayMs`.
- `AsInlineImage` overwrote an already-detected content type with `image/png`, mislabelling
  every non-PNG inline image.
- AWS SES dropped `ReplyTo` on both send paths and, on the raw path, also dropped the priority
  header and sent inline images as ordinary attachments. Azure dropped `ReplyTo` and never set
  an attachment's content ID, so inline images did not render there either.
- `PostmarkClientWrapper` and `BrevoSender` accepted a `CancellationToken` and then called
  their vendor clients without it; the Liquid and Handlebars renderers ignored theirs.
- `AddMailVolt` now registers logging, so transports that take an `ILogger<T>` resolve in a
  host that has not called `AddLogging`. AutoConfigure with `Transport: "Postmark"` previously
  threw at resolve time.
- `MimeMessage` was never disposed in the SMTP and AWS SES senders, nor was Mailgun's
  `MultipartFormDataContent` or its response. The Razor registration added one
  `DiagnosticListener` instance under two service descriptors, so the container disposed it
  twice.
- `ConfigureAwait(false)` is applied throughout, enforced by CA2007. The SMTP and batch paths
  previously marshalled back to the caller's `SynchronizationContext`.
- `AwsSesSender` built a new client on every send, discarding its HTTP connection pool.
- The Liquid renderer re-parsed on every render; the Handlebars cache was unbounded. Both are
  now cached with a cap.
- Every `csproj` overwrote rather than appended to the shared `NoWarn`, so the repo-wide
  warning policy was silently lost and `dotnet restore` failed on two published advisories.
  Vulnerability warnings are no longer suppressed at all.
- `ContinuousIntegrationBuild` is set on CI, so the published `.snupkg` symbol packages work
  for consumers. Each package now ships its own README rather than the root one.

### Changed

- Dependencies updated to their latest stable versions; `Microsoft.SourceLink.GitHub` removed
  as redundant since .NET 8 bundles Source Link in the SDK.
- CI cancels superseded runs, tests on Windows as well as Linux, and actually runs the
  integration tests, which no job had ever executed.

## [0.1.0-preview.3] - 2026-06-20

### Changed

- Replaced the proprietary `FluentAssertions` dependency with `AwesomeAssertions`, the community-maintained Apache-2.0 fork that keeps the same API and namespaces. This removes the commercial license risk for all MailVolt packages and consumers.

## [0.1.0-preview.2] - 2026-06-20

### Fixed

- `HandlebarsTemplateRenderer` now resolves relative template paths against `AppContext.BaseDirectory` when the file is not found in the current working directory.

### Added

- Adopted [MinVer](https://github.com/adamralph/minver) for automatic tag-based versioning.
- Added `CHANGELOG.md` to track release notes.
- Added `build.sh` and `release.sh` local automation scripts.
- Added per-package `README.md` files for every NuGet package.
- Added Dependabot configuration for automated dependency updates.
- Added CodeQL security analysis workflow.
- Added `dotnet format --verify-no-changes` check to CI.
- Added `.github/ISSUE_TEMPLATE/config.yml`, `FUNDING.yml`, and label sync workflow.
- Added SMTP integration tests using Testcontainers.

### Changed

- Fixed `.editorconfig` to recommend file-scoped namespaces, matching the codebase.
- Updated `SECURITY.md` supported-versions table to reflect the current pre-1.0 state.
- Fixed NuGet symbol package publishing by including `.snupkg` files in the release artifact.
- Corrected `docs/release.md` workflow filename reference for NuGet Trusted Publishing setup.

## [0.1.0-preview.1] - 2026-06-16

### Added

- Initial preview release of MailVolt.
- `MailVolt.Core` with async DI-first email builder and batch sender.
- Transports: SMTP (MailKit), SendGrid, Mailgun, Resend, Postmark, Azure Email, Brevo, AWS SES.
- Templates: Razor, Liquid, Handlebars.
- `MailVolt.AutoConfigure` for zero-code `appsettings.json` setup.
- `MailVolt.Testing` with `InMemorySender`, `FailingSender`, and FluentAssertions extensions.

[Unreleased]: https://github.com/deniscuciuc/mailvolt/compare/v0.1.1...HEAD
[0.1.1]: https://github.com/deniscuciuc/mailvolt/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/deniscuciuc/mailvolt/compare/v0.1.0-preview.3...v0.1.0
[0.1.0-preview.3]: https://github.com/deniscuciuc/mailvolt/compare/v0.1.0-preview.2...v0.1.0-preview.3
[0.1.0-preview.2]: https://github.com/deniscuciuc/mailvolt/compare/v0.1.0-preview.1...v0.1.0-preview.2
[0.1.0-preview.1]: https://github.com/deniscuciuc/mailvolt/releases/tag/v0.1.0-preview.1
