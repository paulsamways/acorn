#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")
ENV_FILE="$PROJECT_DIR/deploy/.env"

if [ ! -f "$ENV_FILE" ]; then
    echo "Missing $ENV_FILE"
    exit 1
fi

# deploy/.env uses compose-style names; map them to the names the app reads (see docker-compose.yml)
env_value() {
    grep -m1 "^$1=" "$ENV_FILE" | cut -d= -f2-
}

docker run --rm \
    -p 8080:8080 \
    -v acorn-data:/app/data \
    -e DataDirectory=/app/data \
    -e SenderName="$(env_value SENDER_NAME)" \
    -e SenderEmailAddress="$(env_value SENDER_EMAIL_ADDRESS)" \
    -e SmtpServer="$(env_value SMTP_SERVER)" \
    -e SmtpPort="$(env_value SMTP_PORT)" \
    -e SmtpUsername="$(env_value SMTP_USERNAME)" \
    -e SmtpPassword="$(env_value SMTP_PASSWORD)" \
    acorn
