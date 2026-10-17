# UUID Primary-Key Implementation Plan

> **Status:** Implementation complete; the fresh migration baseline, automated validation, and synthetic storage comparison are complete. Representative insert/read timing and production-workload measurements remain future validation.
>
> This plan assumes the project may replace its migration history and start from an empty schema. It does not plan a data-preserving migration from the current schema. Existing databases and backups must be treated as incompatible with the replacement migration baseline unless separately exported and restored.

## Decision

- Use a single `Guid Id` for each application aggregate root or independently exposed entity. The same UUID is the database primary key and the stable identifier used by routes, application contracts, and data exchange. Do not retain a parallel integer PK and `PublicId`.
- Generate new identifiers as UUIDv7 in the application using .NET's UUIDv7 API. UUIDv7's time-ordered prefix improves insertion locality compared with random UUIDv4 values. The random component makes identifiers impractical to enumerate, but IDs are never authorization credentials.
- Persist GUID-valued primary keys and their foreign keys in SQLite as 16-byte BLOBs. Use one explicitly verified RFC/network byte ordering for every such property. This preserves UUIDv7 ordering when SQLite compares BLOB keys.
- Use SQLite `WITHOUT ROWID` only for tables where it lowers total storage. Current measured candidates are `blob`, `content`, and `file`. Keep `content_attachment`, `user`, and `role` as ordinary rowid tables because their secondary indexes make UUID primary keys more expensive in `WITHOUT ROWID` tables.
- Preserve domain-defined natural keys. `BlobEntity` remains keyed by its 32-byte SHA-256 digest; do not add a UUID merely for uniformity.
- Keep framework-owned dependent Identity entity keys (integer or composite) unless an entity is independently exposed or is an aggregate root. Identity `User` and `Role` already have GUID primary keys; use BLOB storage and verify their ID generation and every GUID foreign key.

This replaces the two-identifier design currently used by `ContentEntity` and `FileEntity`. It also makes content, files, and attachments consistent without forcing a synthetic UUID onto content-addressed blobs or framework-owned join/claim rows.

## Current model and affected surfaces

| Area | Current shape | Planned shape |
|---|---|---|
| Content (`ContentEntity`, TPH `PostContentEntity`, `NoteContentEntity`, `BookmarkContentEntity`) | Integer `Id` plus UUID `PublicId`; content relationships use integer IDs | One UUIDv7 `Id`; remove `PublicId`; all content FKs use UUID |
| Files (`FileEntity`, TPH `ImageFileEntity`, `BinaryFileEntity`) | Integer `Id` plus UUID `PublicId`; owner is a GUID; file references use integer IDs | One UUIDv7 `Id`; remove `PublicId`; all file FKs use UUID |
| `ContentAttachmentEntity` | UUID `Id` generated with `Guid.NewGuid()`; `ContentId` and `FileId` are integers; `ParentId` is a nullable UUID | UUIDv7 `Id`; content/file FKs become UUID; parent FK remains UUID and uses the same BLOB encoding |
| Identity `User` and `Role` | GUID primary keys; GUID foreign keys in Identity tables | Keep GUID keys, persist all GUID properties as BLOB, and verify UUIDv7 generation |
| Identity dependent tables | Framework-managed integer or composite keys, with GUID references where applicable | Keep their key shapes; convert their GUID references consistently |
| `BlobEntity` | 32-byte digest BLOB primary key | Keep the digest key; assess `WITHOUT ROWID` for this table |

The existing IDs also appear in `Content`/`Post`/`Note`/`Bookmark` models, not-found exceptions, content and file service interfaces and implementations, admin controllers/view models/forms, and route templates. Public post lookup currently resolves `PublicId`; file and attachment references also use UUIDs. All of these must converge on `Id` without changing the actual UUID value contract for newly created records.

## Implementation phases

### 1. Verify UUID conversion and SQLite schema support

Do this as a small model/test spike before changing the entity graph.

- Define one GUID-to-byte-array converter that writes and reads exactly 16 bytes in RFC/network byte order. Do not assume the built-in `GuidToBytesConverter` is suitable: .NET's traditional `Guid.ToByteArray()` layout has mixed-endian fields, which can disrupt UUIDv7 lexical ordering when stored as BLOBs.
- Verify that the converter round-trips GUIDs, preserves UUID version/variant bits, and emits bytes that correspond to canonical UUID text order. Include a UUIDv7 case and verify that UUIDs with distinct timestamps sort chronologically when SQLite orders their encoded BLOB values. Do not assert strict ordering for IDs generated within the same timestamp interval.
- Verify the .NET UUIDv7 generator produces version 7 identifiers and configure a single generation path for application-owned entities. Audit Identity's actual user/role creation path; configure it if the framework does not generate UUIDv7 IDs itself.
- Configure and verify conversion for GUID primary keys, nullable GUIDs, and GUID foreign keys. Test both inserts and translated equality lookups so EF parameters use the same representation as stored keys.
- The EF Core 11 SQLite provider does not model `WITHOUT ROWID` directly. The implementation records a custom table annotation and uses `AcornSqliteMigrationsSqlGenerator` to emit it. The generated model snapshot retains the setting, and migration tests verify the table SQL; do not replace this with a one-off schema edit.
- Inspect the resulting SQLite schema (`sqlite_master`, `PRAGMA table_info`, and `PRAGMA foreign_key_list`) and assert key columns are BLOBs, foreign keys target the intended columns, and selected tables are `WITHOUT ROWID`.

**Gate — passed:** ordered BLOB encoding, EF key/FK conversion, UUIDv7 generation, and `WITHOUT ROWID` generation are covered by `UuidSchemaTests`.

### 2. Standardize aggregate and exposed entity IDs

- Change `ContentEntity.Id` to UUIDv7 and remove `PublicId`. Its derived TPH types inherit that key; they do not get separate IDs.
- Change `FileEntity.Id` to UUIDv7 and remove `PublicId`. Its derived TPH types inherit that key.
- Keep `ContentAttachmentEntity.Id` as its externally referenceable key, but generate new values as UUIDv7. Change `ContentId` and `FileId` to UUIDs; retain nullable `ParentId` as a UUID self-reference.
- Keep `User.Id` and `Role.Id` as GUIDs; verify both are generated as UUIDv7 for newly created records.
- Keep `BlobEntity.Digest` as the natural primary key. Do not invent an additional `Id`.
- Do not add UUID IDs to dependent Identity claim, login, token, or join rows merely for visual consistency. Keep their existing framework-defined key structures, while converting GUID-valued references to BLOB.
- Ensure UUID creation occurs before persistence so IDs are available for relationships, returned models, URLs, and export/import without depending on database-generated values.

### 3. Convert persistence and relationships to BLOB UUIDs

- Apply the verified converter consistently to all GUID-valued model properties stored by SQLite, including `Guid?` values and Identity references. Keep the model-level type as `Guid`; the BLOB conversion is a persistence concern.
- Set the SQLite store type to `BLOB` for these converted values. Keep `BlobEntity.Digest` as its existing byte-array BLOB and keep non-GUID values unchanged.
- Make `ContentAttachmentEntity.ContentId` and `FileId` UUID foreign keys to the new root IDs, and retain the self-referencing attachment FK. Verify delete behaviors and existing query filters still apply.
- Configure `WITHOUT ROWID` for `blob`, `content`, and `file`. Keep `content_attachment`, `user`, and `role` as ordinary rowid tables due to their multiple secondary indexes. Re-evaluate only when actual data or index patterns change.
- Remove the `PublicId` property mappings and their unique indexes. The primary key now provides uniqueness.
- Verify the EF model snapshot describes converter/provider types and `WITHOUT ROWID` metadata correctly.

### 4. Propagate UUID IDs through application contracts and routes

- Change service interfaces and implementations that accept integer entity IDs to accept `Guid`. This includes content CRUD/publish operations, content attachment creation, and file lookup (`IFileManager.OpenFileAsync`).
- Update content models and mapping: replace integer `Id` with `Guid Id` and remove `Post.PublicId`. Update not-found exceptions to report the single UUID identity.
- Update admin controller parameters, route constraints/templates, view models, and Razor forms/links to carry UUID IDs. Keep the user-facing route shapes where practical, changing only the route parameter type/name as needed.
- Update public post lookup and file/attachment URL generation to use `Id`. Verify serialized/public models do not expose a stale integer identifier or duplicate public-ID field.
- Update ordering tie-breakers and any query predicates that currently compare or sort by integer IDs.
- Update fixtures, test helpers, test data, and assertions that assume integer IDs or a separate `PublicId`.

### 5. Replace the migration baseline

- Remove the existing migration history and scaffold a new initial migration from the completed model. Do not write a migration that copies old `PublicId` values into new PKs or converts an existing database in place.
- Treat existing database files as incompatible with the new baseline. The accepted rollout is to back up any data that matters, then create a fresh database from the new initial migration. Do not let startup attempt to apply the new initial migration to a database that has the old schema/history.
- Update migration tests that target old migration IDs. Test applying the new baseline from an empty SQLite database and verify model snapshot parity.
- Review the generated DDL for all PKs, FKs, indexes, BLOB types, delete actions, check constraints, and `WITHOUT ROWID`. Avoid hand-editing generated migration files unless the provider requires a documented, repeatable schema operation.
- Confirm the README's backup/restore guidance is clear for databases created under the new baseline; old backups are not automatically compatible.

### 6. Verify behavior, integrity, and storage effects

- Add focused converter tests for GUID round-trip, exact byte order, UUIDv7 version/variant, nullable values, FK values, and timestamp ordering in SQLite.
- Add model/schema tests proving every intended UUID PK/FK is BLOB, every PK/FK relationship is enforced, UUID IDs are unique, and selected tables are `WITHOUT ROWID`.
- Add service tests for creating and querying content, files, attachments, and Identity records with UUID IDs; include cascade behavior, soft-delete filters, uniqueness, and authorization paths.
- Update controller/route tests and verify public post URLs, file references, and admin CRUD workflows bind UUIDs correctly. Verify malformed IDs follow the application's normal not-found/validation behavior.
- Run existing entity, service, file, backup, and migration suites, then the full test suite and application build.
- Compare representative SQLite databases built with the old and new key layouts using the same row counts and secondary indexes. Record `page_count`/`page_size` and representative insert/read timings. Confirm that BLOB plus `WITHOUT ROWID` reduces space as intended and does not cause a material regression in important queries.
- Initial synthetic storage check (10,000 content rows, 10,000 file rows, and 30,000 attachment rows; 4 KiB pages; representative FK and lookup indexes): legacy integer PK plus text UUIDs used 7,942,144 bytes; UUID BLOB keys with `WITHOUT ROWID` on all three tables used 8,122,368 bytes; UUID BLOB keys with `WITHOUT ROWID` on content/file only used 7,651,328 bytes; UUID BLOB keys with rowid tables used 7,901,184 bytes. This fixture supports applying `WITHOUT ROWID` selectively, not universally; actual production workload measurements remain the final arbiter.
- The synthetic comparison recorded database size, not representative insert/read latency. Run those timings against representative production-like data before treating the storage trade-off as fully benchmarked.
- Run `PRAGMA foreign_key_check` after migration creation and after CRUD/cascade integration tests.

## Risks and decisions to retain

- UUIDv7 exposes an approximate creation time. Its random portion is not a substitute for authorization; all existing ownership, publication, and attachment access checks remain required.
- The byte converter is part of the database key contract. Changing byte order later is a schema/data migration, so lock it down with tests before creating the new baseline.
- `WITHOUT ROWID` is not automatically smaller for every schema. Secondary indexes and composite keys can change the trade-off; validate table-by-table.
- Resetting migrations is intentionally destructive to existing local databases unless data is exported separately. This plan does not include a production-safe upgrade path from the old schema.
