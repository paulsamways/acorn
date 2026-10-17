#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")

SOLUTION="$PROJECT_DIR/src/Solution.slnx"
CLIENT_PROJECT="$PROJECT_DIR/src/Acorn.Client/"

dotnet build "$SOLUTION" --no-incremental

cd "$CLIENT_PROJECT"
npx webpack build --mode production