#!/usr/bin/env bash
set -euo pipefail

# Strict validation: fail if any required secret or input is missing or empty
required_vars=(
  SECRET_TG_TOKEN
  SECRET_TG_USER_ID
  SECRET_GOOGLE_SHEET_ID
  SECRET_GEMINI_API_KEY
  SECRET_GEMINI_MODEL
  SECRET_CATEGORIES
  SECRET_GOOGLE_CREDENTIALS
  CONTAINER_NAME
  IMAGE_TAG
)

for var in "${required_vars[@]}"; do
  if [ -z "${!var:-}" ]; then
    echo "::error::Required secret or variable '${var#SECRET_}' is missing or empty!"
    exit 1
  fi
done

# Write .env file
printf "CONTAINER_NAME=%s\nIMAGE_TAG=%s\nTG_TOKEN=%s\nTG_USER_ID=%s\nGOOGLE_SHEET_ID=%s\nGEMINI_API_KEY=%s\nGEMINI_MODEL=%s\n" \
  "$CONTAINER_NAME" \
  "$IMAGE_TAG" \
  "$SECRET_TG_TOKEN" \
  "$SECRET_TG_USER_ID" \
  "$SECRET_GOOGLE_SHEET_ID" \
  "$SECRET_GEMINI_API_KEY" \
  "$SECRET_GEMINI_MODEL" > .env

if [ -n "${SECRET_APPLICATIONINSIGHTS_CONNECTION_STRING:-}" ]; then
  printf "APPLICATIONINSIGHTS_CONNECTION_STRING=%s\n" "$SECRET_APPLICATIONINSIGHTS_CONNECTION_STRING" >> .env
fi

# Prepare deploy directory and configuration files
mkdir -p deploy
printf '%s\n' "$SECRET_CATEGORIES" > deploy/categories.yaml
printf '%s\n' "$SECRET_GOOGLE_CREDENTIALS" > deploy/google-api-credentials.json
