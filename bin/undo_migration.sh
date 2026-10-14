#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")

EF_PROJECT="$PROJECT_DIR/src/Acorn.Core"
STARTUP_PROJECT="$PROJECT_DIR/src/Acorn"

dotnet ef migrations remove --project "$EF_PROJECT" --startup-project "$STARTUP_PROJECT"
