#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 0.1.0"
    exit 1
fi

if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?$ ]]; then
    echo "Invalid SemVer version: $VERSION"
    exit 1
fi

cd "$(dirname "$0")"

TAG="v${VERSION}"

echo "==> Checking the working tree is clean..."
if [ -n "$(git status --porcelain)" ]; then
    echo "Working tree has uncommitted changes. Commit or stash them first:"
    git status --short
    exit 1
fi

echo "==> Checking the current branch..."
BRANCH="$(git rev-parse --abbrev-ref HEAD)"
if [ "$BRANCH" != "main" ]; then
    echo "Releases are cut from main, not '$BRANCH'."
    exit 1
fi

echo "==> Checking the tag does not already exist..."
git fetch --tags --quiet
if git rev-parse -q --verify "refs/tags/${TAG}" >/dev/null; then
    echo "Tag ${TAG} already exists."
    exit 1
fi

echo "==> Checking main is in sync with origin..."
git fetch origin main --quiet
LOCAL="$(git rev-parse HEAD)"
REMOTE="$(git rev-parse origin/main)"
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "HEAD ($LOCAL) does not match origin/main ($REMOTE)."
    echo "Push or pull first, so the released commit is the one on origin."
    exit 1
fi

# The release workflow builds its notes from this section, so an empty one is a hard error
# rather than a release with no notes.
echo "==> Verifying CHANGELOG has a non-empty entry for ${VERSION}..."
NOTES="$(awk -v v="$VERSION" '
    $0 ~ "^## \\[" v "\\]" { found = 1; next }
    found && /^## \[/ { exit }
    found { print }
' CHANGELOG.md)"
if [ -z "$(echo "$NOTES" | tr -d '[:space:]')" ]; then
    echo "No CHANGELOG.md section for ${VERSION}, or the section is empty."
    exit 1
fi

echo "==> Running full build..."
./build.sh

echo
echo "About to tag and push ${TAG} at ${LOCAL}."
echo "This triggers .github/workflows/release.yml, which publishes to nuget.org."
echo "Publishing a version to NuGet is irreversible."
echo
echo "Release notes:"
echo "$NOTES"
echo
read -r -p "Continue? [y/N] " reply
if [[ ! "$reply" =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 1
fi

echo "==> Tagging ${TAG}..."
git tag -a "${TAG}" -m "Release ${TAG}"

echo "==> Pushing tag..."
git push origin "${TAG}"

echo "==> Release ${TAG} triggered."
if command -v gh >/dev/null 2>&1; then
    gh run watch --exit-status "$(gh run list --workflow=release.yml --limit 1 --json databaseId --jq '.[0].databaseId')" || true
fi
