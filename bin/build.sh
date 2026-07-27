#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")
STARTUP_PROJECT="$PROJECT_DIR/src/Acorn"

dotnet build "$STARTUP_PROJECT"
