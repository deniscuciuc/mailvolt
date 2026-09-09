#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

echo "==> Restoring packages..."
dotnet restore

# Same gate CI applies, so a green local build means a green CI run.
echo "==> Checking formatting..."
dotnet format --verify-no-changes --no-restore

echo "==> Building (Release, warnings as errors)..."
dotnet build --configuration Release --no-restore /p:TreatWarningsAsErrors=true

# All three target frameworks, not just net10.0 — otherwise this can pass while CI fails.
echo "==> Running unit tests on net8.0, net9.0 and net10.0..."
dotnet test --configuration Release --no-build --filter "Category!=Integration"

if [ "${SKIP_INTEGRATION:-}" = "1" ]; then
    echo "==> Skipping integration tests (SKIP_INTEGRATION=1)"
elif docker info >/dev/null 2>&1; then
    echo "==> Running integration tests..."
    dotnet test tests/MailVolt.Integration.Tests --configuration Release --no-build \
        --framework net10.0 --filter "Category=Integration"
else
    echo "==> Skipping integration tests (no Docker daemon reachable)"
fi

echo "==> Packing..."
rm -rf ./artifacts
dotnet pack --configuration Release --no-build --output ./artifacts

echo "==> Done. Packages in ./artifacts"
