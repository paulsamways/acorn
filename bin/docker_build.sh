#!/bin/bash

PROJECT_DIR=$(readlink -f "$(dirname "$(realpath  "$BASH_SOURCE")")/..")

docker build -f "$PROJECT_DIR/deploy/acorn.Dockerfile" -t acorn "$PROJECT_DIR"
