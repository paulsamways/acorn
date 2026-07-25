#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")
STARTUP_PROJECT="$PROJECT_DIR/src/Acorn"

cd $STARTUP_PROJECT
dotnet watch
