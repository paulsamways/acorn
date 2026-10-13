# Attachment Derivations

## Goal

When an attachment is created, keep the uploaded file as the parent/original. The caller may optionally name a derivation pipeline, which is selected using the validated input metadata; the current image pipeline accepts `ImageFileMetadata` under the name `DefaultImageContentAttachment`.

When explicitly requested, the image pipeline creates large, medium, and small WebP renditions. Their longest edges are capped at 4096, 1200, and 320 pixels respectively; images are not upscaled or cropped, and all are encoded at WebP quality 82. Public renditions normalize orientation and strip EXIF, XMP, and IPTC metadata. The original is owner-only when a pipeline runs. If no pipeline is named, no derivations are created and the original remains public. The create operation returns the parent's attachment ID, not a child ID.

Generation and serving are separate: serving selects only already-generated attachments and never generates a missing derivation.

## Current Design and Gaps

- `ContentService.CreateAttachmentAsync` checks content ownership and validates the stream. It runs the named pipeline only when the caller supplies one, writes the original and any returned outputs, saves the attachment graph, and returns the root ID. The admin post-image upload explicitly requests the default image pipeline.
- `ContentAttachment` has `ParentId`, `DerivationName`, `Visibility`, nullable `IsPreferredDerivation`, and a parent/derivations relationship. Roots have no parent or derivation name and a null preference; child derivations point to the root and have an explicit true/false preference.
- The database has a unique index on `(ParentId, DerivationName)` and a filtered unique index allowing at most one preferred child per parent. Application validation also rejects duplicate normalized child names and preferred private children.
- `Visibility` supports `Public`, `Authenticated`, and `Owner`. Its enum default is `Public`, so new root and child visibility values should be set deliberately rather than relying on implicit defaults.
- `OpenAttachmentAsync` accepts an optional `derivationName`; explicit names select that child, while a missing name uses the original for its owner and otherwise applies the public default-selection policy below.
- `FileValidator` validates supported image formats and returns `ImageFileMetadata`. `FileManager.WriteFileAsync` stores the original and derived files as content-addressed blobs and reuses tracked blob rows when outputs have identical bytes.
- Validation and derivation require seekable streams. `CreateAttachmentAsync` uses the caller's seekable stream directly or copies a non-seekable stream to a delete-on-close temporary file, then restores positions between stages.
- The previous ImageSharp sketch in `docs/todo.md` has been replaced with a pointer to this document and the pipeline implementation.

## Explicitly Out of Scope for This Iteration

Do not add validation or policy for maximum upload size, maximum input pixel count, frame handling, or CPU/time budgets as part of attachment derivations. Retain existing validator and decoder behavior; do not expand or redefine its frame handling here. Large or pathological inputs may therefore still consume substantial resources. Track resource limits as a separate follow-up rather than making them a prerequisite for this feature.

## Proposed Design

### 1. Define pipeline contracts

Add a small file-management abstraction for configured derivations:

- A pipeline has a stable name and declares the input metadata type it accepts.
- A registry resolves exactly one applicable pipeline using the validated metadata and requested pipeline name. No match, ambiguous matches, or unsupported metadata should produce a clear, explicit error. Omitting the pipeline name is an intentional request to skip derivation, not an error.
- A pipeline receives a replayable input stream and validated metadata, and returns zero or more named derivation results. A pipeline exception or missing/ambiguous registration fails attachment creation; a successful empty result is valid.
- Each result should carry its normalized derivation name, output stream, output `FileMetadata`, visibility, and whether it is preferred for default public serving. Keep this separate from `FileMetadata`, which currently describes a file rather than attachment-serving policy.
- The pipeline name is `DefaultImageContentAttachment`. Output names are lower-case `large`, `medium`, and `small`; all generated image derivations are public, and `medium` is preferred when present.

Keep pipeline implementation independent of `ContentService` and persistence. The service should orchestrate selection, output validation/metadata, file writes, attachment rows, and saving; image processing should not know about EF entities.

### 2. Implement the image pipeline

Start with a named image pipeline for supported `ImageFileMetadata`. Reuse ImageSharp and the existing image metadata model rather than adding a separate image stack.

For every generated output:

1. Load the source using the existing validator and decoder behavior. Do not add maximum upload size, pixel-count, frame-policy, or CPU/time-budget validation in this iteration.
2. Apply EXIF orientation before calculating dimensions or resizing.
3. Produce the large (4096 px), medium (1200 px), and small (320 px) maximum-longest-edge renditions. Preserve aspect ratio, do not crop, and never upscale.
4. Re-encode all outputs as WebP at quality 82 instead of copying source metadata.
5. Strip EXIF, XMP, and IPTC profiles from every public output; verify no other personally identifying metadata survives in the encoded output.
6. Populate output metadata from the encoded result (filename, media type, dimensions, format). Do not copy stale input dimensions or claim WebP if the bytes are another format.
7. Mark the generated safe renditions as public and mark `medium` preferred when it is present. A successful pipeline may return a subset or no outputs; reject multiple preferred results or a preferred non-public result.

The input `ImageFileMetadata` validates image identity and dimensions. This feature does not add input-size, input-pixel-count, frame-policy, or CPU/time-budget validation; resource exhaustion from oversized or pathological inputs remains a known, accepted risk for this iteration.

### 3. Orchestrate `CreateAttachmentAsync`

Keep the existing checks in this order: load content, reject missing content, verify the current user owns it, then validate the file. After validation:

1. Make the input replayable for validation, original storage, and derivation processing. Use the caller's seekable stream or a delete-on-close temporary file for a non-seekable input. Define stream ownership and disposal; do not introduce a maximum-upload-size check in this iteration. Restore positions and honor cancellation throughout.
2. If a pipeline name was supplied, resolve and run that pipeline for the validated metadata before storing files. A pipeline failure aborts creation; zero or a subset of outputs is allowed when the pipeline succeeds. If no name was supplied, skip pipeline resolution, execution, and output validation entirely.
3. Write the original file and create the root attachment explicitly with `ParentId = null`, `DerivationName = null`, and `IsPreferredDerivation = null`. Set `Visibility = Owner` when a pipeline was requested, and `Visibility = Public` otherwise. The create operation returns this root ID.
4. Validate pipeline results before writing rows: derivation names are non-empty, normalized consistently (including case), and unique within the root; at most one public child is preferred.
5. Write each output using its output metadata. Create one child attachment per result with `ParentId` set to the root ID, the normalized `DerivationName`, and the declared visibility/preferred policy.
6. Save the root and all child rows together in one database transaction and return the root attachment ID only after save succeeds.

Only callers that request a pipeline receive derivations. With no pipeline, the public original is served directly to eligible non-owners. Do not fall back to an unprocessed original after an explicitly requested pipeline fails.

### 4. Extend attachment policy and persistence

The existing parent relation and `DerivationName` represent named derivations; `Visibility` represents access scope, and nullable `IsPreferredDerivation` persists a preference only for child derivations. A database constraint requires roots to have a null preference and derivations to have an explicit preference. Persisting preference makes selection deterministic and allows future pipeline changes without silently changing existing attachments.

Define and enforce these invariants:

- Root: no parent, no derivation name, and not marked preferred.
- Child: has a parent and a normalized derivation name; belongs to the same content as the root; is not itself a parent unless multi-level derivations are explicitly supported.
- At most one preferred child per root. A successful pipeline may return no preferred child.
- `DerivationName` matching uses one canonical case/normalization rule for creation and lookup. The HTTP query parameter should also be named `derivationName`.
- A preferred child is not automatically public: preference is only considered among children whose visibility is `Public`.

The migration adds the preferred marker, the partial unique index, and checks preventing a root or private child from being preferred. Keep the EF model snapshot aligned with these constraints; application validation remains in place for clear errors before persistence.

### 5. Define serving and authorization semantics

Preserve explicit-name lookup behavior while adding a clear default-selection rule:

- With an explicit `derivationName`, find exactly the child with that `DerivationName` under the requested root and apply that child's visibility policy. If it is unavailable or access is denied, the HTTP result is 404; do not substitute another derivation.
- Without `derivationName`, the owner receives the original. A non-owner may receive the original if its visibility permits; otherwise, published content can select among public child derivations. Select the preferred public child if present; if none is preferred, select the first public derivation ordered by `DerivationName` using ordinal ordering. If no public derivation exists, return 404.
- Never serve an unpublished content item's public child to a non-owner. Publication remains a gate independent of attachment visibility.
- Owners may continue to access their own original/children according to existing owner behavior. Authenticated and owner-only visibility checks must not be weakened by fallback.
- Only mark a response publicly cacheable when the selected attachment is public and its content is published. Private/owner responses remain `private, no-store`.
- If no attachment is eligible, preserve the existing not-found response behavior at the controller boundary rather than disclosing whether a private attachment or a missing derivation exists.

No public original or child may expose unpublished content to a non-owner. A private original does not make a public sanitized derivation private; this is intentional and protected by the content publication check.

The HTTP query parameter is `derivationName`. Omit it for default selection; owners receive the original, and other requesters receive the eligible public default. Explicitly naming an unavailable derivation returns not found.

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Original image metadata or unsafe dimensions leak publicly | Serve only re-encoded, stripped public renditions when the original is not public; test actual output metadata and dimensions. |
| Non-seekable or large upload streams cannot be replayed safely | Define replay storage and stream ownership; test seekable and non-seekable inputs, cancellation, and cleanup without adding upload-size validation. |
| Oversized or pathological images consume excessive resources | Accepted known risk for this iteration; track maximum input size, pixel count, frame handling, and CPU/time budgets as separate follow-up work. |
| A pipeline failure leaves incomplete database rows or orphaned blobs | Save attachment rows in one DB transaction; account for blob storage being external and non-transactional. Use temporary/staged blobs or a cleanup/reconciliation strategy for failed writes rather than assuming the DB transaction rolls back blob writes. |
| Duplicate/invalid output names break the unique index or make lookup ambiguous | Normalize names once, reject duplicates before persistence, and test case-insensitive collisions. |
| Multiple or absent preferred outputs yield unstable/unsafe default responses | Validate a deterministic preferred-public policy and handle missing preference explicitly; never substitute an arbitrary child. |
| Fallback weakens authorization or exposes unpublished content | Apply content publication and selected-child visibility checks on every request; test anonymous, authenticated, owner, unpublished, and published cases. |
| Cached content outlives visibility/publication changes | Keep public cache headers limited to published, public selections and retain the current short cache lifetime; consider invalidation requirements if policy changes become frequent. |
| An explicitly requested pipeline is missing or mismatched | Fail explicitly and surface an operationally actionable error; do not catch and downgrade to the original. |
| Generated files or blobs become orphaned after cancellation/save failure | Define cleanup ownership and test cancellation/failure at each write/save boundary. Blob reference counting and garbage collection may be needed because blob storage is content-addressed and shared. |

## Implementation Sequence

1. Register the default image pipeline and resolve it by name and validated metadata type when a caller opts in.
2. For opted-in requests, generate and validate safe WebP outputs, then persist the original and successful pipeline outputs as one attachment graph. For other requests, persist a public original only.
3. Apply request-time selection independently from generation: owner original, explicit named lookup, and public preferred/name-ordered fallback.
4. Keep the database model and migration aligned with derivation naming and preferred-child constraints.
5. Verify processing, persistence, authorization, and fallback behavior. Do not add input resource-limit validation in this iteration.

## Verification Plan

### Creation

- Missing content and non-owner creation requests fail before file writes or derivation.
- Invalid/unsupported images fail validation without creating attachment rows.
- The root ID is returned; the root has no parent/name/preferred marker and is owner-only when a pipeline is requested, public otherwise.
- Omitting the pipeline name does not resolve or invoke a pipeline, creates no children, and leaves the original public.
- An explicit pipeline name runs that pipeline; pipeline errors fail creation rather than downgrading to a public original.
- Large, medium, and small outputs are WebP with expected dimensions, public visibility, and medium preferred.
- Duplicate names after normalization, invalid metadata, multiple/private preferred outputs fail clearly; zero pipeline outputs succeed.
- Pipeline selection handles supported metadata, unsupported metadata, unknown names, and ambiguous registrations deterministically.
- Seekable and non-seekable streams, cancellation, processing failures, and database-save failures have defined behavior and no incomplete attachment graph. Do not add maximum-upload-size, maximum-pixel-count, frame-policy, or CPU/time-budget validation.

### Serving and authorization

- Owners receive the original by default; non-owners receive a permitted public original or the preferred public child.
- If there is no preferred public child, default selection uses the first public derivation by ordinal name; if no public derivation exists, it returns not found.
- Explicit `derivationName` selects only the child with that name; a private derivation is not replaced by a public one.
- Unpublished content is not exposed to non-owners even when a child is public.
- Owner, authenticated, and anonymous access preserve their intended visibility rules.
- Missing/private derivations remain indistinguishable at the HTTP boundary; cache headers reflect the selected file's actual public eligibility.

### Image safety and persistence

- Verify output bytes and metadata, not only pipeline result flags: orientation is applied, longest-edge dimensions are capped at 4096/1200/320, encoding and MIME agree, quality is 82, and personal profiles are absent from every public rendition.
- Verify database migration up/down, model snapshot, parent-child deletion behavior, unique derivation-name behavior, and existing attachment compatibility.
- Run focused content-service and file-management tests, then the repository's standard test and build scripts.
