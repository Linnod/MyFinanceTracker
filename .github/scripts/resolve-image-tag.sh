#!/usr/bin/env bash
set -euo pipefail

RESOLVED_TAG=$(git log -n 1 --pretty=format:%H -- . ':!*.md' ':!*.MD' ':!docs/**' ':!.gitignore' ':!.env.example' ':!LICENSE')

if [ -z "$RESOLVED_TAG" ]; then
  RESOLVED_TAG="${GITHUB_SHA:?GITHUB_SHA is not set}"
  echo "No matching commits found in git history; falling back to GITHUB_SHA: $RESOLVED_TAG"
else
  echo "Resolved IMAGE_TAG from last relevant commit: $RESOLVED_TAG"
fi

echo "IMAGE_TAG=$RESOLVED_TAG" >> "$GITHUB_ENV"
