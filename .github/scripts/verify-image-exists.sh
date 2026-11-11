#!/usr/bin/env bash
set -euo pipefail

if [ -z "${IMAGE_NAME:-}" ] || [ -z "${IMAGE_TAG:-}" ]; then
  echo "::error::IMAGE_NAME and IMAGE_TAG environment variables are required!"
  exit 1
fi

IMAGE="ghcr.io/${IMAGE_NAME}:${IMAGE_TAG}"
echo "Checking if container image $IMAGE exists in GHCR..."

FOUND=false
for i in {1..24}; do
  if docker manifest inspect "$IMAGE" > /dev/null 2>&1; then
    echo "Container image $IMAGE is available in GHCR! Proceeding with deploy."
    FOUND=true
    break
  fi
  echo "Container image not ready yet (attempt $i/24). Waiting 5 seconds..."
  sleep 5
done

if [ "$FOUND" = "false" ]; then
  echo "::error::Container image $IMAGE not found in GHCR. Make sure CI has successfully run and built the image for commit $IMAGE_TAG."
  exit 1
fi
