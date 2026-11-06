#!/usr/bin/env bash
set -euo pipefail

# Expand ~ if present in target directory
TARGET_PATH="${TARGET_DIR/#\~/$HOME}"

if [ -d "$TARGET_PATH" ]; then
  echo "Stopping container for project '$PROJECT_NAME'..."
  (cd "$TARGET_PATH" && docker compose -p "$PROJECT_NAME" down || true)

  echo "Removing target directory $TARGET_PATH..."
  rm -rf "$TARGET_PATH"

  docker image prune -f
  echo "=== Environment $TARGET_DIR ($PROJECT_NAME) completely destroyed and cleaned up! ==="
else
  echo "Directory $TARGET_PATH does not exist, nothing to clean up."
fi
