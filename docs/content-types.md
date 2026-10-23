# Content Type Development Documentation

This guide describes how to add a new content type to Acorn. It is written for both maintainers and coding agents; follow the existing type patterns and keep the model, service, persistence, UI, and tests in sync.

## How Content Is Stored

`Content` is the stable identity and ownership root for a post, bookmark, or note. Its revision history is stored separately in `ContentRevision`; authored fields, tags, and revision-specific attachment visibility belong to revisions. A content item has zero or one draft revision, and `PublishedRevisionId` selects the currently public revision.

The content roots and revisions each use EF Core table-per-hierarchy (TPH) mapping. The `content` table stores the stable root and its `content_type`; the `content_revision` table stores versioned fields and its `revision_type`. Both use shared primary keys and type-specific lower-snake-case revision columns, such as `note_value`, `post_title`, and `bookmark_url`.

Content IDs remain stable across revisions and are used by routes and relationships. They are identifiers, not authorization tokens; access checks remain mandatory.

Soft-deleted content is excluded by the global query filter in `ContentEntityConfiguration`. Normal list and lookup queries should rely on this filter. Public queries must select only the revision referenced by `PublishedRevisionId`; a null pointer means the content is not publicly available.

## Add a Content Type

1. **Create the entities.** Add a content-root subtype and a corresponding revision subtype under `Acorn.Core/ContentManagement/Database/Entities`. Put authored fields and tags on the revision, not the stable root.

2. **Add discriminator values.** Add stable, lowercase values for the content-root subtype in `ContentEntityConfiguration` and the revision subtype in `ContentRevisionEntityConfiguration`.

3. **Configure the entities.** Configure root metadata in `ContentEntityConfiguration` and versioned fields in a revision configuration. Map revision fields to `content_revision` with lowercase snake-case names and mark required fields with `.IsRequired()`.

4. **Expose a DbSet.** Add an internal `DbSet<YourContent>` property to `ApplicationDbContext`. This makes the intended query surface clear even though all types share one physical table.

5. **Add a display model.** Add a public record under `Acorn.Core/ContentManagement/Models` containing the fields the UI needs, including tags and draft/published metadata. Do not expose EF entities to controllers or views.

6. **Define the service contracts and implementations.** Provide a public `I<Type>Service`/`<Type>Service` that exposes only the revision selected by `PublishedRevisionId`, and an `I<Type>AuthoringService`/`<Type>AuthoringService` for CRUD, listing, publishing, and soft-delete operations. Authoring edits use the existing draft, or create one from the current published revision (falling back to the highest revision number). A filtered unique index permits at most one draft per content item. Throw `ContentNotFoundException` for missing entities. Normalize tags through `TagSet`; persist `tags?.ToList() ?? []`. Keep content-specific validation in the authoring service as well as the web model.

7. **Register the service.** Add the implementation to `AddContentManagement` in `Acorn.Core/Extensions/ServiceCollectionExtensions.cs`.

8. **Add authoring UI.** Create an admin view model, controller actions, routes, and Razor views under the admin area. The editor accepts a content ID, not a revision selector; it edits the existing draft or creates one if needed. Validate required fields and URL formats with data annotations, show validation errors, and use `TagSet.Parse` for tags. Provide create, edit, publish, and archive/delete actions as appropriate.

9. **Add public UI when appropriate.** Public controllers should use published-only service queries and explicitly allow anonymous access where intended. Keep details for drafts inaccessible. Render user-entered plain text as encoded text; use `Html.Raw` only for content deliberately rendered through the configured Markdown pipeline.

10. **Sort tags for display.** `TagSet` is unordered. Render tags with ordinal alphabetical ordering, for example `tags.OrderBy(tag => tag, StringComparer.Ordinal)`.

11. **Add tests.** Cover entity persistence, service create/update/read behavior, tag normalization, publication filtering, not-found behavior, and soft deletion. Add web validation or controller tests for important user-facing constraints.

12. **Update the schema.** After changing the entity, discriminator, and type configuration, run the migration helper from the repository root, for example `./bin/add_migration.sh AddBookmarkContent`. This scaffolds a migration and updates the model snapshot; it does not apply the migration. Review both `Up` and `Down` and verify the snapshot before applying it. Since content uses TPH, new derived-type columns are nullable in the shared `content` table so rows for other types remain valid, even when the derived entity property is required. The application applies migrations at startup. Do not hand-edit generated migrations, and do not generate one when the task explicitly asks to defer schema changes.

13. **Run verification.** Run `./bin/test.sh` and `./bin/build.sh`. Check `git diff --check` and confirm the model snapshot has no unintended schema differences.

## Current Conventions

- Shared table: `content`
- Discriminator column: `content_type`
- Shared key column: `content_id`
- Shared tag column: `tags`, normalized by `TagSet`
- Inherited timestamps and foreign key columns use lowercase snake case
- Derived columns use a type prefix in lowercase snake case
- Published public reads use `PublishedRevisionId`
- Deleted rows are hidden by the inherited query filter
- Persisted tags are a list of normalized strings; tag ordering is a presentation concern
