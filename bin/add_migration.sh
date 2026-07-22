#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")

if [ -z "$1" ]; then
    echo "Usage: $(basename "$0") <name>"
    exit 1
fi

NAME="$1"
EF_PROJECT="$PROJECT_DIR/src/Acorn.Core"
EF_OUTPUT_DIR="Data/Migrations"
STARTUP_PROJECT="$PROJECT_DIR/src/Acorn"

dotnet ef migrations add "$NAME" --project "$EF_PROJECT" --output-dir "$EF_OUTPUT_DIR" --startup-project "$STARTUP_PROJECT"
