#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath "$BASH_SOURCE")")/..")
SOLUTION="$PROJECT_DIR/src/Solution.slnx"

cd "$PROJECT_DIR/src" || exit
dotnet test --solution "$SOLUTION"
