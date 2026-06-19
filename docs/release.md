# Releasing MailVolt

This guide covers how MailVolt packages are built, versioned, and published to NuGet.org.

## Versioning

MailVolt follows [Semantic Versioning 2.0](https://semver.org/):

- `MAJOR.MINOR.PATCH` for stable releases (e.g. `1.0.0`).
- Pre-release versions append a label (e.g. `0.1.0-preview.1`).

Because MailVolt is not yet battle-tested, the first public releases should use a `0.x` version with a pre-release label:

```text
0.1.0-preview.1
```

Recommended pre-release labels, from earliest to most stable:

| Label | Meaning |
|-------|---------|
| `alpha` | Very early, likely incomplete |
| `beta` | Feature-complete but still testing |
| `preview` | Usable preview; good for first public releases |
| `rc` | Release candidate |

NuGet sorts pre-release labels alphabetically, so `alpha.1` < `beta.1` < `preview.1` < `rc.1`. Increment the trailing number for each iteration (`preview.1`, `preview.2`, etc.).

## Release workflow

Releases are fully automated via a single GitHub Actions workflow:

- `.github/workflows/release.yml` — builds, tests, packs, creates a GitHub release, and publishes the packages to NuGet.org.

### Creating a release

1. Make sure `main` is green:

   ```bash
   dotnet build --configuration Release /p:TreatWarningsAsErrors=true
   dotnet test tests/ --configuration Release --filter "Category!=Integration" --framework net10.0
   ```

2. Create and push a version tag:

   ```bash
   git tag v0.1.0-preview.1
   git push origin v0.1.0-preview.1
   ```

   The package version is derived from the tag by [MinVer](https://github.com/adamralph/minver), so the NuGet package version becomes `0.1.0-preview.1`.

3. `release.yml` automatically:
   - Builds and tests the tagged commit.
   - Packs all NuGet packages.
   - Creates a GitHub release with auto-generated notes.
   - Attaches `.nupkg` and `.snupkg` files as release assets.
   - Publishes the packages to NuGet.org using Trusted Publishing.

## Publishing to NuGet.org

MailVolt uses **NuGet Trusted Publishing** (OIDC) instead of a long-lived API key. The workflow requests a short-lived OIDC token from GitHub Actions, exchanges it with NuGet.org for a temporary API key, and pushes the packages with that key.

### One-time setup

1. **Configure a Trusted Publisher on nuget.org**

   - Sign in to [nuget.org](https://nuget.org).
   - Go to **Account** → **Trusted publishers** → **Add trusted publisher** → **GitHub Actions**.
   - Enter:
     - **Owner**: `deniscuciuc`
     - **Repository**: `mailvolt`
     - **Workflow filename**: `release.yml`
     - **Environment**: leave blank unless you are using a GitHub environment
   - Save the policy.

2. **Add the NuGet username repository variable**

   In the GitHub repository:

   - Go to **Settings** → **Secrets and variables** → **Actions** → **Variables** → **New repository variable**.
   - Name: `NUGET_USERNAME`
   - Value: your nuget.org username (not your email)

3. **Remove any old API key secret**

   If you previously stored a `NUGET_API_KEY` secret in the repository, delete it. It is no longer needed and should not be used.

### How it works

The publish workflow performs these steps:

1. Downloads `.nupkg` and `.snupkg` assets from the published GitHub release.
2. Runs `NuGet/login@v1`, which exchanges the GitHub OIDC token for a short-lived NuGet API key.
3. Pushes packages with `dotnet nuget push` using the temporary key.

No long-lived secret is stored in GitHub, and the temporary key expires shortly after the workflow run.

## Notes

- Symbol packages (`.snupkg`) are uploaded alongside `.nupkg` files. `dotnet nuget push` on a `.nupkg` automatically publishes the matching symbol package if it is present.
- Integration tests requiring live credentials are excluded from the release build.
- Release notes should clearly state if a version is a preview, e.g.:

  > This is a preview release. Public APIs may change before 1.0.
