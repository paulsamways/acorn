# Acorn

## Architecture

### Entity Framework entities

`Acorn.Core` is organised as vertical slices (e.g. `FileManagement`, `ContentManagement`). The public surface of the assembly is its service interfaces and the types they use; persistence is an internal detail.

- **Location:** each slice owns its entities and their configurations:
  - Entities: `Acorn.Core.<Slice>.Database.Entities`
  - Configurations: `Acorn.Core.<Slice>.Database.EntityTypeConfigurations`
  - Example: `Acorn.Core.FileManagement.Database.Entities.FileEntity`
- **Naming:** entity classes are suffixed with `Entity` (`FileEntity`, not `File`) to distinguish them from domain/DTO types. Configurations are named `<Name>EntityConfiguration`.
- **Visibility:** entities and their configurations are `internal`. They must never appear in public service signatures; services map them to public models/DTOs.

## SQLite Backups

All application data is stored beneath `DataDirectory` (default `data`, relative to the app content root). Development overrides this to `../../var`, placing data in the repository-root `var` directory. The SQLite database is `acorn.db`; backups and content-addressed blobs are stored in configurable relative directories, defaulting to `backups` and `blobs`. Configure `DatabaseBackup__DirectoryPath` and `FileSystemContentAddressedBlobStore__DirectoryPath` to change those paths. Nested relative paths are allowed, but paths must remain under `DataDirectory`. Set `DataDirectory` to an absolute path to choose another location. The Docker Compose deployment sets it to `/app/data`, which is backed by the persistent `acorn-data` volume.

The app creates one SQLite online backup each day at 04:00 Sydney time (AEST/AEDT), including after restarts. By default it keeps the 14 most recent backups. Backup filenames contain a UTC timestamp. Configure `DatabaseBackup__RetentionCount` to change how many backups are retained.

To restore, stop the app, replace the database file with a selected backup, and restart. Startup migrations will run against the restored database. Local backups share the database volume and are not protection from loss of that volume; copy them elsewhere for off-host disaster recovery.

## Attributions
