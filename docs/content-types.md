# Content Type Development Documentation

This guide describes how to add a new content type to Acorn. It is written for both maintainers and coding agents; follow the existing type patterns and keep the model, service, persistence, UI, and tests in sync.

## How Content Is Stored

`Content` is the base entity for content that has an author, creation/update timestamps, publication state, deletion state, and tags. `NoteContent`, `PostContent`, and `BookmarkContent` derive from it.

The entities use EF Core table-per-hierarchy (TPH) mapping: every type is stored in the `content` table, and the `content_type` discriminator identifies each derived type. The shared primary key is `content_id`. Derived fields use type-specific lower-snake-case column names to avoid collisions in the shared table, such as `note_value`, `post_title`, and `bookmark_url`.

Soft-deleted content is excluded by the global query filter in `ContentEntityTypeConfiguration`. Normal list and lookup queries should rely on this filter. Public listing queries must additionally require `PublishedAt != null`.

## Add a Content Type

1. **Create the entity.** Add an internal sealed class under `Acorn.Core/Data/Entities` deriving from `Content`. Declare type-specific properties as required or nullable according to the domain. Shared tags already come from `Content`; do not redeclare them.

2. **Add the discriminator value.** Add `.HasValue<YourContent>("your_content")` to the discriminator in `ContentEntityTypeConfiguration`. Use a stable, lowercase discriminator string.

3. **Configure the entity.** Add an `IEntityTypeConfiguration<YourContent>` in `Acorn.Core/Data/EntityTypeConfigurations`. Map it to `content` and explicitly name every derived column in lowercase snake case, usually prefixed with the content type. Mark required properties with `.IsRequired()`. Keep the shared `content_id` and inherited column mappings in `ContentEntityTypeConfiguration`.

4. **Expose a DbSet.** Add an internal `DbSet<YourContent>` property to `ApplicationDbContext`. This makes the intended query surface clear even though all types share one physical table.

5. **Add a display model.** Add a public record under `Acorn.Core/ContentManagement/Models` containing the fields the UI needs, including shared tags and publication timestamps. Do not expose EF entities to controllers or views.

6. **Define the service contract and implementation.** Follow `INotesService` and `NotesService` for CRUD, listing, publish, and soft-delete operations. Throw `ContentNotFoundException` for missing entities. Normalize tags through `TagSet`; persist `tags?.ToList() ?? []`. Public queries must return published records only. Keep content-specific validation in the service as well as the web model.

7. **Register the service.** Add the implementation to `AddContentManagement` in `Acorn.Core/Extensions/ServiceCollectionExtensions.cs`.

8. **Add authoring UI.** Create an admin view model, controller actions, routes, and Razor views under the admin area. Validate required fields and URL formats with data annotations, show validation errors, and use `TagSet.Parse` for tags. Provide create, edit, publish, and archive/delete actions as appropriate.

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
- Published public lists filter on non-null `PublishedAt`
- Deleted rows are hidden by the inherited query filter
- Persisted tags are a list of normalized strings; tag ordering is a presentation concern
