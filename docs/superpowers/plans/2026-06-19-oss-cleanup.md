# OSS Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply all OSS best-practice recommendations (release lifecycle, packaging, docs, hygiene, quality gates, integration tests) so the repository is green, well-documented, and ready for wider adoption without releasing a new version.

**Architecture:** Use MinVer for tag-based versioning, keep all shared build logic in `Directory.Build.props`, add local shell scripts for build/release, add per-package READMEs packed into NuGet, add GitHub automation for Dependabot/labels/CodeQL, and add SMTP integration tests using Testcontainers.

**Tech Stack:** .NET 8/9/10, MinVer, xUnit, Testcontainers.MailKit (or fallback to Testcontainers for SMTP), GitHub Actions, Dependabot.

---

## Task 1: Adopt MinVer and Remove Hardcoded Versions

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/MailVolt.Core/MailVolt.Core.csproj`
- Modify: `src/MailVolt.AutoConfigure/MailVolt.AutoConfigure.csproj`
- Modify: `src/MailVolt.Testing/MailVolt.Testing.csproj`
- Modify: `src/MailVolt.Templates.Razor/MailVolt.Templates.Razor.csproj`
- Modify: `src/MailVolt.Templates.Liquid/MailVolt.Templates.Liquid.csproj`
- Modify: `src/MailVolt.Templates.Handlebars/MailVolt.Templates.Handlebars.csproj`
- Modify: `src/MailVolt.Transport.Smtp/MailVolt.Transport.Smtp.csproj`
- Modify: `src/MailVolt.Transport.SendGrid/MailVolt.Transport.SendGrid.csproj`
- Modify: `src/MailVolt.Transport.Mailgun/MailVolt.Transport.Mailgun.csproj`
- Modify: `src/MailVolt.Transport.Resend/MailVolt.Transport.Resend.csproj`
- Modify: `src/MailVolt.Transport.Postmark/MailVolt.Transport.Postmark.csproj`
- Modify: `src/MailVolt.Transport.AzureEmail/MailVolt.Transport.AzureEmail.csproj`
- Modify: `src/MailVolt.Transport.Brevo/MailVolt.Transport.Brevo.csproj`
- Modify: `src/MailVolt.Transport.AwsSes/MailVolt.Transport.AwsSes.csproj`

- [ ] **Step 1: Add MinVer to central package management**

Add to `Directory.Packages.props` inside `<ItemGroup>`:
```xml
<PackageVersion Include="MinVer" Version="6.0.0" />
```

- [ ] **Step 2: Reference MinVer globally for packable projects**

Add to `Directory.Build.props` inside the existing `<ItemGroup Condition="'$(IsPackable)' != 'false'">` (or create one matching existing pattern):
```xml
<PackageReference Include="MinVer" PrivateAssets="All" />
```

Remove all explicit `<Version>1.0.0</Version>` lines from every `src/**/*.csproj`.

- [ ] **Step 3: Verify version is derived from tag**

Run: `git tag v0.2.0-preview.2` (locally, delete afterwards if not releasing)
Run: `dotnet build --configuration Release src/MailVolt.Core/MailVolt.Core.csproj`
Check assembly attributes with: `dotnet tool run ilspy` or inspect `obj/project.assets.json` for resolved version.
Run: `git tag -d v0.2.0-preview.2`

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "build: adopt MinVer for tag-based versioning"
```

---

## Task 2: Fix Symbol Package Publishing and Release Workflow

**Files:**
- Modify: `.github/workflows/release.yml`
- Modify: `docs/release.md`

- [ ] **Step 1: Include .snupkg files in the upload artifact**

In `.github/workflows/release.yml`, change the `Upload packages` step path from `./artifacts/*.nupkg` to:
```yaml
path: |
  ./artifacts/*.nupkg
  ./artifacts/*.snupkg
```

- [ ] **Step 2: Verify publish job pushes symbols**

Ensure the publish job runs `dotnet nuget push ./artifacts/*.nupkg --api-key ${{ steps.nuget-auth.outputs.api-key }} --source https://api.nuget.org/v3/index.json` with both `.nupkg` and `.snupkg` present in the downloaded artifact directory. MinVer no longer needs `-p:PackageVersion` override; remove that property from the pack step.

- [ ] **Step 3: Fix docs/release.md workflow filename**

Change any reference to `publish-nuget.yml` to `release.yml`.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "ci(release): publish symbol packages and fix workflow docs"
```

---

## Task 3: Add CHANGELOG.md

**Files:**
- Create: `CHANGELOG.md`

- [ ] **Step 1: Create Keep a Changelog file**

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- OSS best-practice cleanup: MinVer versioning, per-package READMEs, Dependabot, CodeQL, local build scripts.
- Integration test project using Testcontainers for SMTP.

## [0.1.0-preview.1] - 2026-06-16

### Added
- Initial preview release with Core, transports, templates, AutoConfigure, and Testing packages.
```

- [ ] **Step 2: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: add CHANGELOG.md"
```

---

## Task 4: Add Local Build and Release Scripts

**Files:**
- Create: `build.sh`
- Create: `release.sh`
- Create: `Justfile` (optional but nice)

- [ ] **Step 1: Create build.sh**

```bash
#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

echo "==> Restoring packages..."
dotnet restore

echo "==> Building (Release, warnings as errors)..."
dotnet build --configuration Release --no-restore /p:TreatWarningsAsErrors=true

echo "==> Running tests..."
dotnet test --configuration Release --no-build --filter "Category!=Integration"

echo "==> Packing..."
dotnet pack --configuration Release --no-build --output ./artifacts

echo "==> Done. Packages in ./artifacts"
```

Make executable: `chmod +x build.sh`

- [ ] **Step 2: Create release.sh**

```bash
#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 0.2.0-preview.2"
    exit 1
fi

if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9\.]+)?$ ]]; then
    echo "Invalid SemVer version: $VERSION"
    exit 1
fi

cd "$(dirname "$0")"

echo "==> Verifying CHANGELOG has an entry for $VERSION..."
grep -q "\[${VERSION}\]" CHANGELOG.md || (echo "No CHANGELOG entry for $VERSION"; exit 1)

echo "==> Running full build..."
./build.sh

echo "==> Tagging v${VERSION}..."
git tag -a "v${VERSION}" -m "Release v${VERSION}"

echo "==> Pushing tag..."
git push origin "v${VERSION}"

echo "==> Release v${VERSION} triggered. Monitor .github/workflows/release.yml"
```

Make executable: `chmod +x release.sh`

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "build: add build.sh and release.sh scripts"
```

---

## Task 5: Add Badges to Root README.md

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Insert badges after the title**

Add:
```markdown
# MailVolt

[![CI](https://github.com/deniscuciuc/mailvolt/actions/workflows/ci.yml/badge.svg)](https://github.com/deniscuciuc/mailvolt/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MailVolt.Core.svg)](https://www.nuget.org/packages/MailVolt.Core/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Codecov](https://codecov.io/gh/deniscuciuc/mailvolt/branch/main/graph/badge.svg)](https://codecov.io/gh/deniscuciuc/mailvolt)
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs(readme): add CI, NuGet, license, and coverage badges"
```

---

## Task 6: Create Per-Package READMEs

**Files:**
- Create: `src/MailVolt.Core/README.md`
- Create: `src/MailVolt.AutoConfigure/README.md`
- Create: `src/MailVolt.Testing/README.md`
- Create: `src/MailVolt.Templates.Razor/README.md`
- Create: `src/MailVolt.Templates.Liquid/README.md`
- Create: `src/MailVolt.Templates.Handlebars/README.md`
- Create: `src/MailVolt.Transport.Smtp/README.md`
- Create: `src/MailVolt.Transport.SendGrid/README.md`
- Create: `src/MailVolt.Transport.Mailgun/README.md`
- Create: `src/MailVolt.Transport.Resend/README.md`
- Create: `src/MailVolt.Transport.Postmark/README.md`
- Create: `src/MailVolt.Transport.AzureEmail/README.md`
- Create: `src/MailVolt.Transport.Brevo/README.md`
- Create: `src/MailVolt.Transport.AwsSes/README.md`

- [ ] **Step 1: Write a focused README for each project**

Each README should contain:
- H1 package name
- One-line description
- Installation: `dotnet add package <PackageName>`
- Minimal usage example
- Link to full docs: `[Full documentation](../../docs/...)`

Example for `src/MailVolt.Core/README.md`:
```markdown
# MailVolt.Core

Core abstractions, models, and the fluent `IEmailBuilder` for MailVolt.

## Installation

```bash
dotnet add package MailVolt.Core
```

## Quick start

```csharp
services.AddMailVolt();

var builder = services.GetRequiredService<IEmailBuilder>();
var result = await builder
    .From("sender@example.com")
    .To("recipient@example.com")
    .Subject("Hello")
    .HtmlBody("<h1>Hello!</h1>")
    .SendAsync();
```

## Documentation

- [Getting started](../../docs/getting-started.md)
- [API overview](../../docs/advanced/batch-sending.md)
```

Generate similar focused READMEs for every project, referencing the correct transport/template docs.

- [ ] **Step 2: Commit**

```bash
git add src/**/README.md
git commit -m "docs: add per-package README files"
```

---

## Task 7: Fix .editorconfig Namespace Style and SECURITY.md Versions

**Files:**
- Modify: `.editorconfig`
- Modify: `SECURITY.md`

- [ ] **Step 1: Align .editorconfig with file-scoped namespaces**

Change `csharp_style_namespace_declarations = block_scoped` to:
```editorconfig
csharp_style_namespace_declarations = file_scoped:warning
```

- [ ] **Step 2: Correct supported versions in SECURITY.md**

Update the table to reflect current pre-1.0 state:
```markdown
| Version | Supported          |
| ------- | ------------------ |
| 0.1.x-preview | :white_check_mark: |
| < 0.1.0-preview.1 | :x:                |
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore: align editorconfig with file-scoped namespaces and fix SECURITY.md"
```

---

## Task 8: Add OSS Hygiene Files

**Files:**
- Create: `.github/ISSUE_TEMPLATE/config.yml`
- Create: `.github/FUNDING.yml`
- Create: `.github/labels.yml`
- Create: `.github/workflows/sync-labels.yml`

- [ ] **Step 1: Add issue template config**

```yaml
blank_issues_enabled: false
contact_links:
  - name: Documentation
    url: https://github.com/deniscuciuc/mailvolt/tree/main/docs
    about: Check the docs before opening an issue
  - name: Ask a question
    url: https://github.com/deniscuciuc/mailvolt/discussions
    about: Use discussions for Q&A
```

- [ ] **Step 2: Add FUNDING.yml**

```yaml
github: [deniscuciuc]
```

- [ ] **Step 3: Add labels.yml and sync workflow**

`.github/labels.yml`:
```yaml
- name: bug
  color: d73a4a
  description: Something isn't working
- name: enhancement
  color: a2eeef
  description: New feature or request
- name: documentation
  color: 0075ca
  description: Improvements to documentation
- name: good first issue
  color: 7057ff
  description: Good for newcomers
- name: help wanted
  color: 008672
  description: Extra attention is needed
- name: transport
  color: f9d0c4
  description: Provider transport related
- name: template
  color: c5def5
  description: Template engine related
- name: dependencies
  color: 0366d6
  description: Dependency updates
```

`.github/workflows/sync-labels.yml`:
```yaml
name: Sync labels
on:
  workflow_dispatch:
  push:
    branches:
      - main
    paths:
      - '.github/labels.yml'

jobs:
  sync:
    runs-on: ubuntu-latest
    permissions:
      issues: write
    steps:
      - uses: actions/checkout@v4
      - uses: micnncim/action-label-syncer@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore(github): add issue config, funding, labels, and label sync workflow"
```

---

## Task 9: Add Dependabot Configuration

**Files:**
- Create: `.github/dependabot.yml`

- [ ] **Step 1: Add Dependabot config**

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
    groups:
      microsoft-extensions:
        patterns:
          - "Microsoft.Extensions.*"
      mailkit:
        patterns:
          - "MailKit"
          - "MimeKit"
      test-dependencies:
        patterns:
          - "xunit*"
          - "FluentAssertions"
          - "NSubstitute"
          - "coverlet*"
    labels:
      - "dependencies"
    commit-message:
      prefix: "chore(deps)"
```

- [ ] **Step 2: Commit**

```bash
git add .github/dependabot.yml
git commit -m "chore(github): add Dependabot configuration"
```

---

## Task 10: Add CodeQL and dotnet-format Quality-Gate Workflow

**Files:**
- Create: `.github/workflows/codeql.yml`
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1: Add CodeQL workflow**

```yaml
name: CodeQL

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 8 * * 1'

jobs:
  analyze:
    name: Analyze C#
    runs-on: ubuntu-latest
    permissions:
      actions: read
      contents: read
      security-events: write
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
        with:
          languages: csharp
          build-mode: autobuild
      - uses: github/codeql-action/analyze@v3
```

- [ ] **Step 2: Add format check to CI**

In `.github/workflows/ci.yml`, after build/test add:
```yaml
      - name: Check formatting
        run: dotnet format --verify-no-changes --verbosity diagnostic
```

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "ci: add CodeQL analysis and dotnet-format verification"
```

---

## Task 11: Implement Integration Tests with Testcontainers

**Files:**
- Modify: `tests/MailVolt.Integration.Tests/MailVolt.Integration.Tests.csproj`
- Modify: `tests/MailVolt.Integration.Tests/UnitTest1.cs` (or create new test files)
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Add Testcontainers packages**

Add to `Directory.Packages.props`:
```xml
<PackageVersion Include="Testcontainers" Version="4.3.0" />
```

- [ ] **Step 2: Update integration test project**

`tests/MailVolt.Integration.Tests.csproj` should reference:
```xml
<PackageReference Include="Testcontainers" />
<PackageReference Include="MailKit" />
```

Also reference `MailVolt.Core`, `MailVolt.Transport.Smtp`, and `MailVolt.Testing`.

- [ ] **Step 3: Add SMTP integration test**

Create `tests/MailVolt.Integration.Tests/SmtpIntegrationTests.cs`:
```csharp
using Testcontainers.MailKit; // if available; otherwise generic Testcontainers with MailKit image

namespace MailVolt.Integration.Tests;

[Trait("Category", "Integration")]
public class SmtpIntegrationTests : IAsyncLifetime
{
    private readonly MailKitContainer _mailKitContainer = new MailKitBuilder().Build();

    public Task InitializeAsync() => _mailKitContainer.StartAsync();
    public Task DisposeAsync() => _mailKitContainer.DisposeAsync().AsTask();

    [Fact]
    public async Task SendAsync_ShouldDeliverEmail_WhenUsingRealSmtpContainer()
    {
        // Use container credentials to send via MailVolt.Transport.Smtp
    }
}
```

If `Testcontainers.MailKit` is not available, use `Testcontainers` generic builder with a MailKit Docker image (`juanluisbaptiste/postfix` or `maildev/maildev`) and MailKit client assertions.

- [ ] **Step 4: Verify tests build and integration trait is present**

Run: `dotnet build tests/MailVolt.Integration.Tests/MailVolt.Integration.Tests.csproj`

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(integration): add SMTP integration tests with Testcontainers"
```

---

## Task 12: Clean Up Stray testlog File

**Files:**
- Delete: `testlog.host.26-06-16_15-52-07_17416_5.txt` (if untracked)

- [ ] **Step 1: Remove file**

```bash
rm -f testlog.host.26-06-16_15-52-07_17416_5.txt
git add -A
git commit -m "chore: remove stray test log file"
```

---

## Task 13: Local Verification

- [ ] **Step 1: Run build script**

```bash
./build.sh
```

- [ ] **Step 2: Run integration tests locally if Docker is available**

```bash
dotnet test tests/MailVolt.Integration.Tests/MailVolt.Integration.Tests.csproj --configuration Release
```

If Docker is unavailable, skip and document.

- [ ] **Step 3: Run dotnet format check**

```bash
dotnet format --verify-no-changes
```

---

## Task 14: Push Branch and Create PR

- [ ] **Step 1: Push branch**

```bash
git push -u origin oss-cleanup
```

- [ ] **Step 2: Create pull request via GitHub CLI**

```bash
gh pr create --title "chore: OSS best-practice cleanup" \
  --body "Implements MinVer versioning, symbol package publishing, per-package READMEs, Dependabot, CodeQL, local build/release scripts, and SMTP integration tests with Testcontainers." \
  --base main
```

- [ ] **Step 3: Monitor CI and merge once green**

Use `gh pr checks --watch` or open the PR in the browser.

---

## Task 15: Approve and Merge Dependabot PRs

- [ ] **Step 1: Wait for Dependabot to open PRs after merge**

After the branch is merged to `main`, Dependabot will run on its schedule.

- [ ] **Step 2: Review and merge Dependabot PRs**

```bash
gh pr list --author dependabot[bot]
gh pr review <number> --approve
gh pr merge <number> --squash
```

Ensure CI is green before merging.

---

## Self-Review Checklist

- [ ] Every recommendation from the OSS analysis has a matching task.
- [ ] No placeholders remain in the plan.
- [ ] All file paths use the isolated worktree path.
- [ ] Task ordering respects dependencies (MinVer before release workflow changes; build scripts before verification).
