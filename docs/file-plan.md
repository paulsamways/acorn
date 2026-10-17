# File Storage Implementation Plan

This plan covers the database-backed metadata layer for files stored by Acorn's content-addressed blob store, the upload pipeline that validates and enriches files, derived files (thumbnails and similar), and the relationship needed for inline images in posts. It deliberately does not introduce a `FileContent` subtype of `Content`: a file is an independently owned record that can be attached to any `Content`.

> **Status:** This is the original design proposal. The first implementation slice is complete, but some implementation details differ from this plan (including per-rendition producers, public originals, and filename-based attachment URLs). Its integer-primary-key plus `PublicId` strategy is superseded by the UUID primary-key decision in [pk-uuid-plan.md](./plan/pk-uuid-plan.md). See [derivations.md](./derivations.md) for the authoritative description of current file behavior; the remaining design below is planning material for unimplemented lifecycle work.

## Implementation Slices

1. **Persistence and inline-image vertical slice — Implemented.** Add blob/file/image/attachment persistence and public UUIDs; validate uploads and generate applicable image renditions; upload and attach images to owned content; serve originals and derivations through the filename-based attachment route; render attached Markdown image references; and add the editor upload control. See [derivations.md](./derivations.md) for current upload, URL, and access behavior.
2. **File lifecycle and reusable management operations — Planned.** Add detach, soft-delete and restore with derivative cascades, and administrative purge. Keep lifecycle operations idempotent and enforce same-owner rules in core services. Add lifecycle, cascade, and authorization tests.
3. **Web integration tests and closeout — Planned.** Add the Web test project and verify route behavior, upload size limits, antiforgery, authorization and cache headers, and editor insertion. Run the complete test/build/migration checks and update any remaining directly related documentation.

## Agreed Design

### Core entities

- **`Blob`** is stored bytes, identified by their SHA-256 digest, with a byte size and creation time. There is one row per digest. A blob has no media type, name, or owner: bytes only gain identity and use through a `File`.
- **`File`** is the base type for a file record. It has an internal integer primary key for database relationships and a separate, immutable random UUID public ID for Markdown references and routes. It references one `Blob` and holds the owner, original filename, client-declared media type, server-validated media type, creation time, and nullable `DeletedAt`. Each upload creates its own `File`, even when its bytes already exist. The public UUID is an identifier, not an authorization token.
- **`ContentAttachment`** links a `Content` item to a `File`. It is a join entity (not a `Content` subtype and not a stored file). A file can be unattached or attached to many content items; a content item can have many files. Relationship-specific fields (alt text, display order) can be added later.
- A file can be attached only to content with the same owner (`File.OwnerId == Content.AuthorId`). Cross-owner attachment is prohibited, including for administrative operations.
- Duplicate uploads share a `Blob` but not a `File`. Names, owners, and attachments stay specific to each upload.

### Public IDs

- Keep integer primary keys for both `Content` and `File` as internal database keys, and add a separate immutable random UUID public ID to each model. Put the `Content` public ID on the TPH base so all content kinds follow the same convention.
- Use public UUIDs in public post URLs and file references/routes. Keep admin routes on internal IDs. Do not preserve the current `/posts/{id:int}` URL as a redirect; the public route changes to the UUID form.
- Generate public IDs server-side for new records and backfill a unique UUID for every existing `Content` row before making the column non-null and unique.
- Public IDs are not authorization tokens; all existing publication, ownership, attachment, and deletion checks still apply.

### Two-axis file model

The model separates *what the bytes are* from *how the file came to exist*.

- **Kind axis (type hierarchy):** `File` is mapped with EF Core table-per-hierarchy, following the `Content` convention: a `file` table, a `file_type` discriminator, and type-prefixed nullable columns (for example `image_width`, `image_height`). The initial supported kind is `ImageFile`; other kinds can be added alongside explicit format validation and serving policies. There is no generic binary fallback in the initial upload scope. The upload pipeline chooses the kind from the bytes, never the client.
- **Derivation axis (columns on the base `File`):** a nullable `parent_file_id`, `recipe`, and `recipe_version`. An original has all three null; a derived file has them set. A unique index on `(parent_file_id, recipe)` prevents duplicate derivations.

Derivation is deliberately not a subtype (no `DerivedImageFile`). A derivative's kind is independent of its parent's kind: a PDF preview or an audio waveform is an `ImageFile` but not derived from an image. Making derived-ness a subtype would multiply the kind types. A thumbnail is simply an `ImageFile` with its own width and height.

Rules for derived files (`parent_file_id` is not null):

- The owner is inherited from the parent; access is always checked through the parent.
- They are hidden from normal file listings and cannot be attached to content directly. Only originals get `ContentAttachment` rows.
- Soft delete, restore, and purge cascade from the parent.
- Derivation is one level deep: recipes always take an original as input, never another derivative. This avoids cascade chains and quality loss from repeated re-encoding. The file service enforces that a parent is an original and that the derivative inherits its owner's ID; database foreign-key and check constraints enforce the corresponding row-level invariants. Do not add SQLite triggers for these business rules.
- They are safe to purge and regenerate.

### Media type

The client-declared media type is an untrusted claim, not proof of the file format. Store it separately from the server-validated media type. For the initial upload scope, accept only PNG, JPEG, and static WebP images that ImageSharp can fully decode within configured resource limits. Detect the format from the bytes; if a non-generic client-declared type is supplied, reject it when it contradicts the detected format. Treat a missing or generic `application/octet-stream` declaration as no format claim. Reject unsupported formats and animated WebP rather than accepting them as `BinaryFile`.

Storing and serving are separate policies:

- **Storing:** validate the detected format from the bytes and perform a real image decode. Persist the server-validated media type independently of the client declaration; recipe output media types are also validated against the output bytes.
- **Serving:** use only the server-validated media type. For the initial scope, serve validated raster image derivatives inline and never serve originals inline. An original may be downloaded only by its owner, through `/files/{publicId}/original`, with `Content-Disposition: attachment`; public visitors can fetch only processed derivatives. Send downloads as attachments with `X-Content-Type-Options: nosniff`. Never serve client-supplied HTML or SVG inline from the application's origin, since that is a stored-XSS vector. Filenames must be encoded or sanitized in response headers and never used as paths.

### Upload pipeline

An upload passes through a pipeline of three stages:

1. **Validate** the bytes (size limits, declared type vs content, decodability).
2. **Extract metadata** (for example image dimensions) and choose the `File` subtype.
3. **Derive** files by applying every recipe that applies to the chosen kind (see Derived files and recipes). Derivation happens synchronously during the upload, so a file and all of its derivatives exist together or not at all.

The request body is validated before anything is stored. Obtain a seekable stream (`IFormFile.OpenReadStream()`; `Request.EnableBuffering()` for raw bodies; or copy to a temp file opened with `FileOptions.DeleteOnClose` for other sources), then run all three stages against the buffered copy. Derived outputs are produced into temporary buffered streams and put back through validation and metadata extraction, but nothing is written to the blob store until every stage has succeeded. Only then is the original stream seeked to the start and stored with `IContentAddressedBlobStore.PutBlobAsync`, followed by each derived output. A validation or derivation failure never reaches the blob store. Blob writes and the database transaction are not atomic: a blob-store failure partway through writing, or a database failure after blob writes, can leave unreferenced blobs. Accept this storage-leak risk for the initial implementation; do not attempt compensating deletion, since blobs may be shared. Reconciliation is deferred (see Deferred Work). There is no TOCTOU risk because the validated bytes are a fixed, process-owned buffered copy that is also what is stored.

Stage contract: each stage receives a seekable stream, resets its position when finished, and does not dispose it. Limit each upload to 10 MiB both at the request/multipart boundary and again in the pipeline. Allow only one upload pipeline to validate/decode/derive at a time per app instance; reject another upload while busy with 429 rather than queuing unbounded work. Image decoding is attack surface: read dimensions from the header and reject images exceeding 40 megapixels before decoding; also enforce decoder memory limits to resist decompression bombs.

The imaging library is **SixLabors.ImageSharp** (managed, no native dependencies). It is distributed under the Six Labors Split License, so confirm that the project's use falls within the free terms before adding it. Add the package version to `Directory.Packages.props` (central package management is enabled). Image validation, metadata extraction, and image recipes all use it.

### Derived files and recipes

A derived file is a `File` produced by applying a named recipe to an original (parent) file. Its bytes are stored as a content-addressed blob like any other. The output of a recipe goes back through the same pipeline, so a derived thumbnail is validated and becomes an `ImageFile` with real dimensions.

Example uses of derivation:

- **Image:** resized copies for responsive sizes, other formats (WebP, AVIF), and a re-encoded copy with EXIF metadata (such as GPS location) stripped for security.
- **Audio:** compressed copy, waveform image, transcript text.
- **Video:** poster frame (image), transcodes at other resolutions or bitrates, audio-only track.
- **PDF:** page-1 preview (image), extracted text for search.
- **Office documents:** PDF rendering.
- **SVG:** sanitized copy with scripts removed.
- **Text and Markdown:** rendered or highlighted output.

The output kind often differs from the source kind, which is why derivation is an axis and not a subtype. Re-encoding also produces bytes the application created itself, which can be served inline safely regardless of the client-declared type of the original.

Recipe design:

- A recipe has a stable **name** (for example `thumbnail-256`, `web-1200`), an integer **version** that changes whenever its output would change, an **applicability** check on parent kinds, and an apply operation that takes the parent's seekable stream and returns an output stream and media type.
- Recipes are **named presets defined in code**, not free parameters. Anyone able to request an image could otherwise make the server generate and store unbounded variants. Presets bound work and storage, keep the `(parent_file_id, recipe)` key canonical, and make versioning simple. Arbitrary sizes can be supported later by adding a preset.
- Shape: an `IFileRecipe` with `Name`, `Version`, `CanApply(File parent)`, and `ApplyAsync(...)`, plus a registry that resolves recipes by name.
- **Staleness and regeneration:** a derived file whose `recipe_version` is lower than the registered version is stale. Regenerating writes a new blob and points the same `File` row at it; the old blob becomes unreferenced for the collector. Staleness depends on the version number, not on comparing hashes, because encoders are not guaranteed byte-identical. Because derivatives are created only at upload, bumping a recipe version or adding a new recipe does not affect existing files by itself. Until an administrative backfill regenerates stale or missing derivatives from each original's stored blob (see Deferred Work), continue serving the existing derivative, even when a recipe change is security-related. Thus security-related output changes do not protect existing derivatives until backfill completes; account for that explicitly when planning such a change.
- **Reuse:** if a derivation exists for another file with the same source blob and recipe, the pipeline may reuse its output blob instead of regenerating. Rows are still per-parent, but the output bytes stay one blob.
- **When derivatives are generated:** synchronously during upload, as the third pipeline stage. All derivatives for the file's kind are created before the upload completes, so a visible file always has its derivatives and serving never generates anything. This keeps request handling simple (no locking or negative caching), at the cost of slower uploads.
- **Failures:** if any applicable recipe fails, the whole upload is rejected and nothing is stored. Derivatives such as the EXIF-stripped copy are what make a file safe to display, and serving the unprocessed original instead would bypass them, so a file is never accepted without them. Recipes should therefore be dependable, with limits (pixel counts, output size) that fail fast and are reported clearly to the uploader.
- **Transaction:** the database rows (`Blob` rows for the original and each distinct output, the original `File`, and the derived `File` rows) are written in a single transaction, so partial sets are never visible. The unique `(parent_file_id, recipe)` index is a backstop.

### Lifecycle

- Soft-deleting a `File` sets `DeletedAt`; it remains restorable indefinitely unless an administrator purges it. Keep attachment rows on soft delete so restoring restores prior relationships. Hide deleted files from normal retrieval and attachment.
- Purging a file removes its derived files, attachment rows, and metadata.
- A blob is eligible for collection only when no `File` row references it. Because derivatives are `File` rows, this is the only rule needed. Blob garbage collection itself is a later capability.

### Filenames and media types

- The original filename is preserved for display and download, but sanitized on upload: strip any directory components (both `/` and `\`) and control characters, trim whitespace, reject an empty result, and limit to 255 characters. Do not otherwise alter the name. It is never used to build a filesystem path; blobs are addressed by digest.
- The media type is stored as declared, limited to 255 characters, parsed and normalized to lowercase `type/subtype` without parameters, with `application/octet-stream` as the fallback for a missing or unparseable value.

### Inline images in posts

- A post body refers to an attached image by public file UUID using a custom scheme: `![alt](file:<uuid>)`. The Markdown pipeline (which has raw HTML disabled) is extended to rewrite `file:` image links at render time; any other scheme is untouched, so existing posts render as before.
- The renderer resolves a reference only if the public UUID identifies a file attached to the post being rendered and it is an original, non-deleted `ImageFile`. It emits a URL to the serving route (`/files/{publicId}/{recipe}`) using the default preset for inline images (`web-1200`). Missing, deleted, detached, or non-image references render as a visible broken-image placeholder rather than failing the render.
- The file routes authorize each request: soft-deleted files and their derivatives return 404 to everyone, including the owner, until restored. An original is downloadable only by its owner. A non-deleted derivative is viewable by the owner; anyone else (including anonymous visitors) can view it only when the parent is attached to at least one published, non-deleted content item. Files attached only to drafts are therefore not public. Derivatives follow the parent's access.
- Cache only responses for derivatives that are publicly accessible through a published, non-deleted attachment, using `Cache-Control: public, max-age=300`. Owner-only responses use `Cache-Control: private, no-store`; denied and missing responses must not be cached. A public derivative may remain available from a browser or intermediary cache for up to five minutes after unpublish, detach, or deletion.

## Implementation Plan

### Components and application surfaces

#### Persistence

- Add `Blob`, abstract `File`, `ImageFile`, and `ContentAttachment` entities under `Acorn.Core/Data/Entities`, with explicit EF configurations, `DbSet`s, migration, and snapshot updates. `File` and `Content` keep integer database keys and gain unique public UUIDs; `ContentAttachment` and derivation FKs use internal integer keys.
- Add the `Content` public UUID to its TPH base and backfill existing rows in the migration before enforcing non-null and uniqueness. Public post routes move to UUIDs; the old integer post URL is not retained or redirected.
- Keep the existing `IContentAddressedBlobStore` as the byte store. Add no blob-store delete or enumeration behavior in this scope.

#### Core file services

- Add a file-management service area under `Acorn.Core` for upload, lookup, attach/detach, soft-delete/restore, and administrative purge. Keep persistence and authorization rules in these services; controllers must not manipulate EF entities directly.
- Add the upload pipeline, PNG/JPEG/static-WebP validation and metadata extraction, recipe registry, and the initial inline-image recipe. Register the pipeline, recipe(s), and a single-active-upload gate in `Acorn.Core/ServiceCollectionExtensions.cs`.
- Upload through the existing blob-store interface. The post-scoped upload service validates that the current user owns both the file operation and the target post, then creates and attaches the file before returning its public UUID.
- Add an owner-scoped original-download operation and a derivative lookup operation. Serving only looks up existing data; it never runs a recipe.

#### Web controllers and routes

| Method and route | Surface | Behavior |
| --- | --- | --- |
| `GET /posts/{publicId:guid}` | Existing public `PostsController` | Resolve a published post by its public UUID. Do not keep the old integer route. |
| `POST /admin/posts/{id:int}/files` | Existing authenticated admin `PostsController` | Accept a multipart image upload for the post being edited, enforce the 10 MiB request limit and antiforgery validation, create and attach it, then return its public UUID for editor insertion. Reject when the single upload pipeline is busy. |
| `GET /files/{publicId:guid}/{recipe}` | New public `FilesController` | Serve an existing derivative after owner/public-attachment authorization, media-type and cache policy checks. |
| `GET /files/{publicId:guid}/original` | New `FilesController` | Serve the original as an attachment to its owner only; return not found to other callers. |

Keep existing admin post-edit routes on integer IDs. Add route-name constants to `Acorn/Routes.cs`, use the framework's named-route generation where applicable, and ensure the literal `original` route is not treated as a recipe name. Mark both file GET actions `AllowAnonymous` so public derivatives can be served despite the application's authenticated fallback policy; the service still authorizes each request, and the original action returns 404 unless the caller is the owner.

#### Post editor and rendering

- Extend `Acorn/Areas/Admin/Views/Posts/Edit.cshtml` with a focused image upload control, progress/busy feedback, and an alt-text field. On successful upload, insert `![alt](file:<public-uuid>)` at the cursor in the existing Markdown body editor. There is no separate file library or browser in v1.
- Update the public post content model and lookup to carry/use the public UUID while retaining the internal integer ID needed by admin editing.
- Resolve `file:` image references during post rendering only when the UUID identifies an original, non-deleted `ImageFile` attached to that post. Render the `web-1200` derivative URL; render missing, detached, deleted, or non-image references as a visible broken-image placeholder. Preserve existing Markdown rendering for posts without file references and keep raw HTML disabled.
- Keep file ownership and attachment checks in core services. The upload action is authenticated and covered by the existing antiforgery filter; the file GET actions enforce the documented owner/public access policy.

#### Verification surfaces

- Add Core tests for entity mapping/migrations, upload pipeline and services. Since the repository currently has no Web test project, add `src/Acorn.Tests` referencing `Acorn` for upload and file-serving authorization, route behavior, and antiforgery integration tests; follow the existing TUnit conventions.
- Add post-rendering tests for attached-file resolution, public UUID links, and broken placeholders. Add a focused UI test or integration test that upload success inserts a Markdown reference into the post editor.

### 1. Confirm schema details before scaffolding

- Decide how the 32-byte SHA-256 digest is stored as the blob key (or uniquely indexed column), consistent with `BlobInfo.Digest`.
- Keep the internal integer `File` primary key for foreign keys and add an immutable random UUID public ID with a unique index. Generate it server-side; use it in Markdown and routes, never expose the internal integer key, and never treat the public UUID as authorization.
- Add the same server-generated unique public UUID to the `Content` TPH base while retaining its integer primary key. Update public post routes to use it; do not add compatibility redirects from integer post URLs.
- Apply the filename and media-type rules above (255-character limits) to column definitions and validation. Store the client-declared and server-validated media types separately.
- Set the `ContentAttachment` delete behavior explicitly. Removing a content item or purging a file should clean up relationship rows and never implicitly delete shared blobs or other file records.
- Add a self-referencing FK for `parent_file_id` and a check constraint requiring `parent_file_id`, `recipe`, and `recipe_version` to be either all null or all set, with a positive version. The file service verifies the parent is an original and copies its owner ID; do not add triggers.
- Plan indexes: unique digest, owner lookups, active-file lookups, reverse attachment lookups, and `(parent_file_id, recipe)` unique.
- Implement the access rule defined under Inline images in posts before exposing any image URL: publicly visible only when attached to a published, non-deleted content item.

### 2. Add persistence entities and mappings

- Add `Blob`, `File` (abstract base), `ImageFile`, and `ContentAttachment` under `Acorn.Core/Data/Entities`. Do not derive `File` from `Content` and do not add a discriminator value to `Content`. Add other file kinds only when their formats have explicit validation and serving policies.
- Follow the TPH conventions in `docs/content-types.md`: a shared `file` table, `file_type` discriminator, lowercase snake-case columns, type-prefixed nullable derived columns, and an explicit entity configuration per type.
- `Blob`: digest, size, creation time. `File`: internal integer key, unique immutable random UUID public ID, blob digest FK, owner FK, filename, client-declared media type, server-validated media type, creation time, `DeletedAt`, `parent_file_id`, `recipe`, `recipe_version`, with a query filter hiding soft-deleted rows. `ContentAttachment` and derivation FKs use the internal file key; public references use only the UUID.
- Add `DbSet` properties to `ApplicationDbContext`.

### 3. Create and review the migration

- Scaffold with the repository's migration helper after the model is in place; review `Up`, `Down`, and the snapshot for tables, keys, indexes, FKs, and delete behaviors.
- Verify `ContentAttachment` references the TPH `content` table and that SQLite enforces the relationships. Do not hand-edit generated migrations.

### 4. Implement upload pipeline and file services

- Implement the pipeline stages, kind selection, and recipe registry described above, starting with PNG, JPEG, and static WebP validation/metadata and at least one image recipe. Reject animated WebP and unsupported formats.
- Define services for creating a file from an upload, lookup, attach/detach, soft-delete/restore, and administrative purge. Scope access by owner and exclude soft-deleted files; enforce authorization in the service, not only in the UI. Attaching a file requires its owner to match the content author; prohibit cross-owner attachment even for administrative operations. When creating a derivative, verify the parent has no parent and inherit its owner ID. Attaching an already-attached file and detaching an already-detached file both succeed idempotently.
- Upload flow: buffer the stream, run validate, extract metadata, and derive against the buffered copy (derived outputs held in temporary streams), then seek to the start, store the original blob and each derived output, and in one database transaction insert or reuse each `Blob` row by digest (handling concurrent identical uploads via the unique constraint and a re-query) and create the original `File` plus its derived `File` rows. Surface database failures. Blob-store writes and database commit are not atomic; partial blob-store or database failures may leave unreferenced blobs. Accept this risk for the initial implementation and defer reconciliation; do not delete blobs as compensation because they may be shared.
- Never delete a blob during ordinary file deletion or detachment.

### 5. Serve files and integrate inline post images

- Add serving endpoints implementing the serving policy: an owner-only original download at `/files/{publicId}/original` and a derivative route implementing the inline allowlist, `attachment` disposition, and `nosniff`. Endpoints only serve existing files and derivatives; they never generate them. A request for a recipe that has no derivative returns 404. Anonymous/public requests cannot fetch originals.
- Resolve post image references only to files attached to that post, using the appropriate preset derivative. Define rendering for missing, deleted, or detached references and preserve existing Markdown behavior for content without file references.

### 6. Add tests and validate

- Model tests: digest uniqueness, public UUID generation and uniqueness for files and content, metadata persistence, foreign keys, attachment uniqueness, delete behavior, the soft-delete filter, the `(parent_file_id, recipe)` unique index, and the one-level derivation rule.
- Pipeline tests: declared-type mismatch rejection, missing/generic declared type handling, unsupported-format and animated-WebP rejection, the 10 MiB upload limit, single-active-job saturation and 429 behavior, 40-megapixel and decoder-memory limits, seekability and stage contract, kind selection, recipe applicability and versioning, derived outputs being validated and typed correctly, and a failing recipe rejecting the upload with nothing written to the blob store or database.
- Service tests: identical bytes create distinct `File` rows sharing one `Blob`; owner scoping; same-owner attachment enforcement, idempotent attach and detach; soft-delete, restore, and purge cascading to derivatives; and blob/database coordination failures.
- Serving and post integration tests: derivative allowlist behavior, owner-only original downloads, public inability to fetch originals, authorization for drafts versus published posts, cache headers, stale derivative serving until backfill, and missing-file behavior.
- Run the Core and Web tests and application build; review the migration, snapshot, and `git diff --check`; watch for `File` versus `System.IO.File` ambiguity.

## Deferred Work

- Blob garbage collection and reconciliation of physical blobs that lack database rows. `IContentAddressedBlobStore` cannot enumerate stored blobs today.
- An administrative backfill that regenerates stale (lower `recipe_version`) or missing derivatives from the original's blob, needed whenever a recipe is changed or added.
- Automatic purge of soft-deleted files; the agreed behavior is indefinite restoration until an explicit administrative purge.
- Per-owner storage quotas and usage reporting. The initial scope relies on the per-upload size limit and single-active-pipeline limit.
- Upload UI or a general file browser beyond what inline post images require.
- Recipes for audio, video, PDF, and other kinds beyond the initial image support.
