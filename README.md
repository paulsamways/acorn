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

### Services and managers

Service interfaces are the integration boundary for callers outside `Acorn.Core`; service implementations coordinate application use cases and expose public models rather than persistence entities. Managers are internal Core collaborators used by services to encapsulate shared domain or persistence workflows. They may work directly with internal entities, but they should not be exposed as external APIs or used by callers in place of services.

Content has separate authoring and public service contracts. Authoring services are for create, edit, and publish workflows. Opening an editor uses the existing draft or creates one from the current published revision (falling back to the highest-numbered revision). Public content services expose only the revision selected by the current-published pointer and must never return a draft.

### Entity identifiers

Application aggregate roots and independently exposed entities use one UUIDv7 `Id` as both their database primary key and their stable identifier across routes, exports, and imports. Do not keep a separate integer primary key plus `PublicId`; derived TPH entities share their base entity's ID. `ContentAttachment` is independently addressable and follows the same rule.

UUIDv7 gives IDs a time-ordered prefix for better B-tree insertion locality than random UUIDs, while retaining a large random component that makes IDs impractical to enumerate. IDs are identifiers, not authorization tokens; access checks remain mandatory. UUIDs are generated in the application with .NET's UUIDv7 support.

Persist GUID-valued primary and foreign keys as 16-byte SQLite `BLOB`s using one verified RFC byte order, rather than SQLite's default textual GUID representation. UUIDv7 byte ordering must be preserved so indexed BLOB keys remain time ordered. Use SQLite `WITHOUT ROWID` only for tables where it reduces total storage: currently the digest-keyed `blob` table and the `content` and `file` roots. Secondary indexes on attachment and Identity tables make ordinary rowid tables more space-efficient for those entities. EF Core's SQLite provider does not model this option directly, so [AcornSqliteMigrationsSqlGenerator](./src/Acorn.Core/Database/AcornSqliteMigrationsSqlGenerator.cs) emits it from the table annotation; migration tests verify the generated schema.

Keep natural keys where they are the domain identity: `Blob` remains keyed by its SHA-256 digest. Framework-owned dependent Identity tables may retain their integer or composite keys; GUID user/role keys and their foreign keys use the same BLOB representation. The rationale, entity inventory, migration approach, and verification gates are in [docs/plan/pk-uuid-plan.md](docs/plan/pk-uuid-plan.md).

### Optimistic concurrency

Content revisions use an application-managed GUID concurrency token. EF Core includes the token's original value when updating a revision; if another save has changed it, the update affects no row and EF reports a concurrency conflict. Content roots also use a token for lifecycle changes such as moving the current-published pointer. `ConcurrencySaveChangesInterceptor` assigns a new UUIDv7 token whenever an `IConcurrencyTracked` entity is added or modified. Content services translate EF's concurrency exception into `ContentConcurrencyException`.

Update services accept an optional expected revision token. Supplying the token from the edit form detects changes made since that form was loaded. Omitting it uses the token from the revision fetched by the service, allowing a client-wins update relative to the form while still detecting a concurrent save between the service's fetch and its save.

`IConcurrencyTracked` and the interceptor provide a persistence pattern for other entities that need optimistic concurrency. Such entities must also configure their token as an EF Core concurrency token.

### Content revisions

Posts, bookmarks, and notes share a stable content identity and a TPH revision hierarchy. Authored fields, tags, and revision-specific attachment visibility are stored with revisions. A content item has zero or one draft revision and a nullable `PublishedRevisionId` that selects the live revision. Authoring uses the existing draft, or creates one from the current published revision (falling back to the highest revision number) when editing begins. Publishing marks that draft as published and moves the live pointer; it does not create another draft. Published revisions are immutable, so edits to a later draft do not change public output.

`ContentEntity.PublishedRevisionId` selects the live revision explicitly; publication time is historical metadata and does not select the public version. Publishing marks the draft as published and moves that pointer atomically. Attachment identity and file storage remain stable, while a content-revision attachment association determines whether a file belongs to a particular draft or published revision and what visibility it has. Public attachment access is limited to associations on the currently selected revision.

The field inventory, lifecycle rules, migration behavior, affected surfaces, and validation gates are in [docs/plan/content-revisions-plan.md](docs/plan/content-revisions-plan.md).

## Building

Build the .NET solution from the `src` directory:

```sh
cd src
dotnet build
```

## SQLite Backups

All application data is stored beneath `DataDirectory` (default `data`, relative to the app content root). Development overrides this to `../../var`, placing data in the repository-root `var` directory. The SQLite database is `acorn.db`; backups and content-addressed blobs are stored in configurable relative directories, defaulting to `backups` and `blobs`. Configure `DatabaseBackup__DirectoryPath` and `FileSystemContentAddressedBlobStore__DirectoryPath` to change those paths. Nested relative paths are allowed, but paths must remain under `DataDirectory`. Set `DataDirectory` to an absolute path to choose another location. The Docker Compose deployment sets it to `/app/data`, which is backed by the persistent `acorn-data` volume.

The app creates one SQLite online backup each day at 04:00 Sydney time (AEST/AEDT), including after restarts. By default it keeps the 14 most recent backups. Backup filenames contain a UTC timestamp. Configure `DatabaseBackup__RetentionCount` to change how many backups are retained.

To restore, stop the app, replace the database file with a selected backup, and restart. Startup migrations will run against the restored database. The UUID primary-key migration baseline intentionally starts from an empty schema and does not upgrade the former schema; databases and backups created before this baseline are incompatible and must not be opened by the new version. Export any data that must be retained before recreating the database. Local backups share the database volume and are not protection from loss of that volume; copy them elsewhere for off-host disaster recovery.

In production, `ForwardedHeaders__KnownProxyHost` must name the trusted reverse proxy. The Docker Compose deployment sets it to `caddy`; the app resolves that hostname at startup and trusts only its resolved IP addresses when processing forwarded headers. Restart the app after recreating the proxy container so it can pick up any changed address.

## Attributions
