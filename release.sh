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
if ! grep -q "\[${VERSION}\]" CHANGELOG.md; then
    echo "No CHANGELOG entry for $VERSION"
    exit 1
fi

echo "==> Running full build..."
./build.sh

echo "==> Tagging v${VERSION}..."
git tag -a "v${VERSION}" -m "Release v${VERSION}"

echo "==> Pushing tag..."
git push origin "v${VERSION}"

echo "==> Release v${VERSION} triggered. Monitor .github/workflows/release.yml"
