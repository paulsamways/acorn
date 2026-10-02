# Acorn

## SQLite Backups

The app waits one configured interval before its first SQLite online backup, then creates a backup each interval. By default it keeps the 14 most recent backups under a `backups` directory beside the database file. Backup filenames contain a UTC timestamp.

Configure `DatabaseBackup__Interval` as a .NET `TimeSpan` (default `1.00:00:00`), `DatabaseBackup__RetentionCount` (default `14`), or `DatabaseBackup__BackupDirectory` to choose a different local directory. In Docker, the default location is under `/app/data/backups`, alongside the database in the persistent volume.

To restore, stop the app, replace the database file with a selected backup, and restart. Startup migrations will run against the restored database. Local backups share the database volume and are not protection from loss of that volume; copy them elsewhere for off-host disaster recovery.

## Attributions
