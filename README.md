# Acorn

## SQLite Backups

All application data is stored beneath `DataDirectory` (default `data`, relative to the app content root). Development overrides this to `../../var`, placing data in the repository-root `var` directory. The SQLite database is `acorn.db`; backups and content-addressed blobs are stored in configurable relative directories, defaulting to `backups` and `blobs`. Configure `DatabaseBackup__DirectoryPath` and `FileSystemContentAddressedBlobStore__DirectoryPath` to change those paths. Nested relative paths are allowed, but paths must remain under `DataDirectory`. Set `DataDirectory` to an absolute path to choose another location. The Docker Compose deployment sets it to `/app/data`, which is backed by the persistent `acorn-data` volume.

The app creates one SQLite online backup each day at 04:00 Sydney time (AEST/AEDT), including after restarts. By default it keeps the 14 most recent backups. Backup filenames contain a UTC timestamp. Configure `DatabaseBackup__RetentionCount` to change how many backups are retained.

To restore, stop the app, replace the database file with a selected backup, and restart. Startup migrations will run against the restored database. Local backups share the database volume and are not protection from loss of that volume; copy them elsewhere for off-host disaster recovery.

## Attributions
