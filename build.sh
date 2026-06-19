#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

echo "==> Restoring packages..."
dotnet restore

echo "==> Building (Release, warnings as errors)..."
dotnet build --configuration Release --no-restore /p:TreatWarningsAsErrors=true

echo "==> Running tests (excluding integration tests)..."
dotnet test --configuration Release --no-build --filter "Category!=Integration"

echo "==> Packing..."
rm -rf ./artifacts
dotnet pack --configuration Release --no-build --output ./artifacts

echo "==> Done. Packages in ./artifacts"
