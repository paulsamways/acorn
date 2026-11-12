#!/usr/bin/env bash

set -u

if [[ $# -ne 2 ]]; then
    printf 'Usage: %s <source-directory> <destination-directory>\n' "$(basename "$0")" >&2
    exit 1
fi

SOURCE_DIR=$1
DEST_DIR=$2

if ! command -v sqlite3 >/dev/null 2>&1; then
    printf 'Error: sqlite3 is required but was not found in PATH.\n' >&2
    exit 1
fi

if [[ ! -d "$SOURCE_DIR" ]]; then
    printf 'Error: source directory does not exist: %s\n' "$SOURCE_DIR" >&2
    exit 1
fi

if ! mkdir -p -- "$DEST_DIR"; then
    printf 'Error: could not create destination directory: %s\n' "$DEST_DIR" >&2
    exit 1
fi

if ! DEST_DIR=$(cd -- "$DEST_DIR" && pwd); then
    printf 'Error: could not access destination directory: %s\n' "$2" >&2
    exit 1
fi

shopt -s nullglob
databases=("$SOURCE_DIR"/telemetry-*.db)
if (( ${#databases[@]} == 0 )); then
    printf 'Error: no telemetry database files found in %s\n' "$SOURCE_DIR" >&2
    exit 1
fi

status=0
for source in "${databases[@]}"; do
    filename=${source##*/}
    destination="$DEST_DIR/$filename"

    if [[ -e "$destination" || -L "$destination" ]]; then
        printf 'Error: backup already exists, leaving it unchanged: %s\n' "$destination" >&2
        status=1
        continue
    fi

    if ! temporary=$(mktemp --tmpdir="$DEST_DIR" ".${filename}.XXXXXX"); then
        printf 'Error: could not create temporary backup for %s\n' "$source" >&2
        status=1
        continue
    fi

    if ! sqlite3 "$source" ".backup \"$temporary\""; then
        printf 'Error: SQLite backup failed for %s\n' "$source" >&2
        rm -f -- "$temporary"
        status=1
        continue
    fi

    if mv -n -- "$temporary" "$destination" && [[ ! -e "$temporary" ]]; then
        printf 'Backed up %s to %s\n' "$source" "$destination"
    else
        printf 'Error: could not move backup to %s; the source was left unchanged\n' "$destination" >&2
        rm -f -- "$temporary"
        status=1
    fi
done

exit "$status"
