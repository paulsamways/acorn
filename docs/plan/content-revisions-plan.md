# Content Revisions Implementation Plan

> **Status:** Implemented
>
> This plan covers posts, bookmarks, and notes. All three content types are publishable. It preserves the content identifier used by existing routes while separating editable drafts from published snapshots.

## Decision

- Keep a stable content identity and store its revisions separately.
- A content item can have zero or more published revisions and zero or one editable draft revision.
- Published revisions are immutable. Editing uses the existing draft or creates one from the current published revision; if there is no current published revision, it copies the highest-numbered revision.
- A content item's nullable `PublishedRevisionId` explicitly identifies the revision that is live. Do not infer the live revision from `PublishedAt`: an older revision can remain in history without being the one currently published.
- A filtered unique index on revision `ContentId` enforces at most one draft revision per content item. The draft is found by status; there is no draft pointer on `Content`.
- Publishing marks the draft as published, records its publication time, and moves the content's current-published pointer to it. It does not create another draft.
- Keep the current TPH approach for type-specific revision data: a `ContentRevisionEntity` base with post, bookmark, and note revision subtypes stored in one revision table. Use one stable content root with a fixed content-type discriminator; avoid retaining marker-only content subtypes unless they prove useful to the implementation.
- Keep a revision's `PublishedAt` as historical information and use the content pointer—not that timestamp—to decide what public readers see now.

## Field ownership

Fields that affect a published representation must be read from a revision, not from the stable content root.

| Data | Owner | Notes |
|---|---|---|
| Stable content ID and type | Content | ID remains the route and aggregate identifier. Type is fixed at creation. |
| Original author/owner and creation time | Content | These identify the content item, not a particular wording. |
| Deletion state | Content | Soft-deleting content removes it from public queries regardless of revision history. |
| Current published revision ID | Content | Nullable; the public selector. A null pointer means no revision is currently live. |
| Current draft revision | Revision | Zero or one draft exists per content item; the filtered unique index enforces the maximum. |
| Draft/published state, revision number, timestamps, editor, concurrency token | Revision | A draft is mutable; a published revision is immutable. `PublishedAt` is set when the revision is published. |
| Post title, excerpt, body, and tags | Post revision | All are part of the published snapshot. |
| Bookmark URL, title, description, and tags | Bookmark revision | All are part of the published snapshot. |
| Note value and tags | Note revision | All are part of the published snapshot. |
| Attachments, attachment visibility, and attachment relationships | Revision attachment | These affect rendered content and access, so they must not be shared as mutable content-level state. File records and stored blobs remain independently managed. |

`UpdatedAt` should describe a specific revision's last edit. If a content-level last-activity timestamp is still needed for administrative sorting, define it separately and do not use it to choose or order public versions. Public listings should use the current published revision and its publication time.

## Target model and invariants

### Content identity

Replace the current content row's mixed identity, authored fields, and publication state with a stable content entity containing:

- UUIDv7 `Id` (preserve existing content IDs);
- immutable content type;
- `AuthorId` and `CreatedAt`;
- nullable `DeletedAt`;
- nullable `PublishedRevisionId`;
- a concurrency token for root-level lifecycle changes, if needed to protect pointer/deletion updates.

Use a composite relationship or equivalent validation so the current-published pointer cannot reference a revision belonging to another content item. Keep the content type consistent with every revision subtype.

### Revisions

Add a revision entity hierarchy mapped with TPH to a `content_revision` table. Common revision data includes:

- UUIDv7 revision ID and content foreign key;
- monotonically increasing revision number per content item;
- `Draft` or `Published` state;
- creation/update timestamps and, if available, the editing user;
- application-managed concurrency token;
- `PublishedAt`, null for drafts and set on publication;
- versioned common metadata such as tags.

Type-specific revision data holds the existing post, bookmark, and note authored fields. Add constraints for valid state/timestamp combinations and unique revision numbers per content item. Add a filtered unique index that permits no more than one draft for a content item.

Published revision data and its attachment associations are immutable. To restore an older version, copy it into a new draft and publish that draft; do not repoint to or mutate an old published revision in the initial implementation. This keeps publication history unambiguous.

### Attachments

Keep attachment and file identities stable, and associate attachments with revisions through a `ContentRevisionAttachment` join entity. The association owns revision-specific membership and visibility, so multiple revisions may reference the same file without changing attachment IDs embedded in Markdown. Preserve parent/derivation relationships and existing IDs during migration where practical.

When a draft is first created from a published revision, copy its attachment associations and visibility into the draft, referencing the existing files. Subsequent attachment edits affect only that draft's associations.

Public attachment access is allowed only when the attachment belongs to the revision currently selected by its content's `PublishedRevisionId`, and its visibility permits access. Draft and historical-revision attachments remain owner-only. A stable public permalink to historical attachments is out of scope unless explicitly added later.

## Authoring and publication behavior

1. Creating a post, bookmark, or note creates a content root and its initial draft revision.
2. Opening the authoring editor returns the existing draft; if none exists, it creates and saves one from the current published revision or, when there is no current published pointer, the highest-numbered revision.
3. Saving edits to an existing draft updates only that draft and its revision concurrency token. Public reads remain on the current published revision.
4. Direct updates and attachment changes also create a draft if one is missing, using the same source-selection rule as the editor.
5. Publishing validates the type-specific fields and atomically changes the draft to published, sets `PublishedAt`, and updates `PublishedRevisionId`. A failed validation or concurrency check must leave the previous live revision unchanged.
6. After publishing, no draft remains. When editing resumes, create a new draft from the current published revision; if there is no current published pointer, use the highest-numbered revision.
7. Archiving/deleting content continues to operate on the stable content root and removes the item from public reads. An unpublish action is not required for the initial implementation; the nullable current-published pointer allows one to be added later.

Preserve the existing stable content ID in routes and service calls. Update authoring service contracts and models to distinguish the editable draft from the live revision and to carry the appropriate revision concurrency token. Normalize publish behavior across all three content types; in particular, Notes currently combine saving and optional publishing in one form submission.

## Affected application surfaces

- **Persistence:** content and revision entities, TPH configurations, `ApplicationDbContext`, content attachment relationships, migration and model snapshot.
- **Services/contracts:** post, bookmark, and note create/read/update/publish operations; shared recent-published queries; attachment creation, resolution, authorization, and file URL mapping.
- **Public reads:** published item lookups and listings must resolve only the revision selected by `PublishedRevisionId`. Ordering must use current revision publication metadata, not a draft's `UpdatedAt`.
- **Authoring UI:** edit pages must load and save the draft while clearly showing whether an older revision remains live. Publish actions must publish the draft, not the mutable content root. Update post, bookmark, and note forms consistently.
- **Mapping/rendering:** map revision fields and tags; render Markdown and resolve file links against that revision's attachments. Keep public output and route IDs stable where possible.
- **Concurrency:** reject stale draft edits and conflicting publishes. Protect pointer changes and ensure concurrent requests cannot create two drafts or publish a draft that changed after the editor loaded it.

## Implementation phases

### 1. Lock down invariants and behavior tests

- Add service-level tests for create, edit, and publish behavior for all three content types.
- Specify edge cases: first publication, repeated publication after edits, no-draft state after publication, deleted content, and existing content with no published revision.
- Confirm the initial policy for unpublishing and whether the admin UI needs revision history or restore actions. The data model supports history; browsing/restoring it is not required for the first release.

**Gate:** tests demonstrate that draft edits never change public fields, tags, or attachment access, and publishing switches the live revision without altering the previous published revision.

### 2. Implement the revision persistence model

- Add the content root and TPH revision hierarchy, including discriminator values, type-specific properties, concurrency tokens, and indexes/constraints.
- Move tags and other publishable metadata to revisions.
- Move attachment associations to revisions and ensure derivations remain associated with the same revision as their parent.
- Update `ApplicationDbContext`, EF configurations, migration SQL generation if required, and the model snapshot.
- Test the generated SQLite schema, foreign keys, uniqueness constraints, and current-published same-content relationship.

**Gate:** migrations create the intended schema, and invalid revision states or cross-content pointers cannot be persisted.

### 3. Migrate existing content and attachments

- Preserve each content item's existing ID, type, author, creation/deletion metadata, and existing attachment/file IDs where practical.
- Create one initial revision from each existing row's current type-specific fields and tags.
- For currently published rows, create an initial published revision using the existing `PublishedAt` value and point the content root at it.
- For currently unpublished rows, create an initial draft revision.
- Create an association between each existing attachment and its corresponding initial revision, preserving derivations without duplicating attachment or file/blob data.
- Document the unavoidable limitation: the current schema has no prior snapshots, so edits already made to published content cannot be reconstructed. The current row becomes the initial published snapshot for items currently marked published.
- On downgrade, restore the current published revision (or the draft when nothing is published) to the legacy single-row shape, and retain only that revision's attachment memberships; revision history and other draft-only state cannot be represented by the old schema.
- Keep the migration compatible with the repository's current migration lineage. Pre-baseline databases already documented as incompatible remain outside this migration's scope.

**Gate:** migration tests verify representative published and draft content, tags, ownership, deletion state, attachments, derivations, and file references survive.

### 4. Update authoring services and concurrency

- Refactor create/read/update/publish flows for posts, bookmarks, and notes to use revisions.
- Add draft creation from the current published revision on the first post-publication edit.
- Apply concurrency tokens to draft changes and root publication-pointer changes; translate EF concurrency failures to the existing content concurrency exception pattern.
- Make publish state transition and pointer update one transaction.
- Keep validation rules for bookmark URLs/titles and ensure publish-time validation still runs.

**Gate:** concurrent edits and publishing cannot silently overwrite one another or leave a draft marked published without the live pointer update.

### 5. Update attachments and public reads

- Resolve public content from the current-published pointer; ensure historical published revisions are not accidentally selected just because they have a non-null `PublishedAt`.
- Update recent-content ordering and each published-content query to use the selected revision.
- Update attachment authorization so only attachments on the live revision are public; draft and historical attachments remain owner-only.
- Preserve Markdown rendering and file URL resolution while ensuring attachment IDs resolve to the intended revision association.

**Gate:** public endpoints and attachment tests cover published, draft, historical, deleted, and visibility-restricted cases.

### 6. Update authoring UI and public models

- Update edit input/view models to carry revision concurrency tokens and the draft data being edited.
- Show draft/live state and last publication information in admin views.
- Make publishing consistently publish the current draft for all content types.
- Ensure editing, validation failures, and concurrency conflicts return the correct draft to the form without exposing it publicly.
- Keep existing public URLs and content IDs stable; only change contracts where required to represent draft and current-published state.

**Gate:** MVC/controller tests cover create, edit after publication, save without publishing, publish, and stale form submission for every content type.

### 7. End-to-end validation and documentation

- Run focused content service, attachment, mapping/rendering, migration, and controller tests, then the full solution test/build checks.
- Update architecture documentation to describe the identity/revision split, TPH revision mapping, versioned fields, and publication-pointer semantics.
- Verify a user can publish, continue editing privately, and publish the new version without changing the prior public output before the second publish.

## Out of scope for the initial implementation

- Scheduled or future-dated publication.
- Restoring an old published revision directly; restore by copying it into a new draft instead.
- Public permalinks to historical revisions or attachments.
- Multiple simultaneous drafts, branching, or collaborative editing.
- Per-revision authorization policies beyond the existing owner/public visibility behavior.
