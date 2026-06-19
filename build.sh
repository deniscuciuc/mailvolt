#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

echo "==> Restoring packages..."
dotnet restore

echo "==> Building (Release, warnings as errors)..."
dotnet build --configuration Release --no-restore /p:TreatWarningsAsErrors=true

echo "==> Running tests (excluding integration tests) on .NET 10.0..."
dotnet test --configuration Release --no-build --framework net10.0 --filter "Category!=Integration"

echo "==> Packing..."
rm -rf ./artifacts
dotnet pack --configuration Release --no-build --output ./artifacts

echo "==> Done. Packages in ./artifacts"
