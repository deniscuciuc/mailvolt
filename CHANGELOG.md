# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

### Fixed

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
