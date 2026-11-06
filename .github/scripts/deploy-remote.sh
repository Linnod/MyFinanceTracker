#!/usr/bin/env bash
set -euo pipefail

# Expand ~ if present in target directory
TARGET_PATH="${TARGET_DIR/#\~/$HOME}"
cd "$TARGET_PATH"

if [ "${CLEAN_DATA:-false}" = "true" ]; then
  if [ "$PROJECT_NAME" = "fintracker-prod" ] || [ "$TARGET_PATH" = "$HOME/production" ]; then
    echo "::error::Safety guard triggered: clean_data is strictly forbidden for production!"
    exit 1
  fi

  echo "Cleaning deploy/data directory for fresh test deployment..."
  docker compose -p "$PROJECT_NAME" -f docker-compose.yml -f docker-compose.prod.yml down || true
  rm -rf deploy/data/* || true
fi

mkdir -p deploy/data && chmod 777 deploy/data

if [ -n "${GH_TOKEN:-}" ] && [ -n "${GH_ACTOR:-}" ]; then
  echo "$GH_TOKEN" | docker login ghcr.io -u "$GH_ACTOR" --password-stdin
fi

export IMAGE_TAG="$IMAGE_TAG"

docker compose -p "$PROJECT_NAME" -f docker-compose.yml -f docker-compose.prod.yml pull
docker compose -p "$PROJECT_NAME" -f docker-compose.yml -f docker-compose.prod.yml up -d --force-recreate
sleep 5

if [ -z "$(docker compose -p "$PROJECT_NAME" -f docker-compose.yml -f docker-compose.prod.yml ps -q --filter status=running fintracker-bot)" ]; then
  echo "::error::Container failed to start. Logs:"
  docker compose -p "$PROJECT_NAME" -f docker-compose.yml -f docker-compose.prod.yml logs --tail 50
  exit 1
fi

echo "=== Deployment to $TARGET_DIR ($PROJECT_NAME) completed successfully! ==="
docker image prune -f
docker image prune -a --filter "until=72h" -f
