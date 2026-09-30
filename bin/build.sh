#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")
SOLUTION="$PROJECT_DIR/src/Solution.slnx"

dotnet build "$SOLUTION" --no-incremental
