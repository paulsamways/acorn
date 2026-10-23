using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Database.Migrations;

/// <inheritdoc />
public partial class _20261022202529_AddContentRevisions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "current_published_revision_id",
            table: "content",
            type: "BLOB",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "content_revision",
            columns: table => new
            {
                content_revision_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                content_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                revision_number = table.Column<int>(type: "INTEGER", nullable: false),
                revision_state = table.Column<string>(type: "TEXT", nullable: false),
                published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                concurrency_token = table.Column<byte[]>(type: "BLOB", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                tags = table.Column<string>(type: "TEXT", nullable: false),
                revision_type = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                bookmark_url = table.Column<string>(type: "TEXT", nullable: true),
                bookmark_title = table.Column<string>(type: "TEXT", nullable: true),
                bookmark_description = table.Column<string>(type: "TEXT", nullable: true),
                note_value = table.Column<string>(type: "TEXT", nullable: true),
                post_title = table.Column<string>(type: "TEXT", nullable: true),
                post_excerpt = table.Column<string>(type: "TEXT", nullable: true),
                post_body = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_content_revision", x => x.content_revision_id);
                table.CheckConstraint("ck_content_revision_publication_state", "(revision_state = 'Draft' AND published_at IS NULL) OR (revision_state = 'Published' AND published_at IS NOT NULL)");
                table.CheckConstraint("ck_content_revision_required_type_fields", "(revision_type = 'post' AND post_title IS NOT NULL AND post_body IS NOT NULL) OR (revision_type = 'bookmark' AND bookmark_url IS NOT NULL AND bookmark_title IS NOT NULL) OR (revision_type = 'note' AND note_value IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_content_revision_content_content_id",
                    column: x => x.content_id,
                    principalTable: "content",
                    principalColumn: "content_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "content_revision_attachment",
            columns: table => new
            {
                content_revision_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                content_attachment_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                visibility = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_content_revision_attachment", x => new { x.content_revision_id, x.content_attachment_id });
                table.ForeignKey(
                    name: "FK_content_revision_attachment_content_attachment_content_attachment_id",
                    column: x => x.content_attachment_id,
                    principalTable: "content_attachment",
                    principalColumn: "content_attachment_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_content_revision_attachment_content_revision_content_revision_id",
                    column: x => x.content_revision_id,
                    principalTable: "content_revision",
                    principalColumn: "content_revision_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_content_current_published_revision_id",
            table: "content",
            column: "current_published_revision_id");

        migrationBuilder.CreateIndex(
            name: "IX_content_revision_content_id",
            table: "content_revision",
            column: "content_id",
            unique: true,
            filter: "\"revision_state\" = 'Draft'");

        migrationBuilder.CreateIndex(
            name: "IX_content_revision_content_id_revision_number",
            table: "content_revision",
            columns: new[] { "content_id", "revision_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_content_revision_attachment_content_attachment_id",
            table: "content_revision_attachment",
            column: "content_attachment_id");

        migrationBuilder.AddForeignKey(
            name: "FK_content_content_revision_current_published_revision_id",
            table: "content",
            column: "current_published_revision_id",
            principalTable: "content_revision",
            principalColumn: "content_revision_id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(
            """
            INSERT INTO content_revision (
              content_revision_id, content_id, revision_number, revision_state, published_at,
              concurrency_token, created_at, updated_at, tags, revision_type,
              bookmark_url, bookmark_title, bookmark_description,
              note_value, post_title, post_excerpt, post_body)
            SELECT
              content_id, content_id, 1,
              CASE WHEN published_at IS NULL THEN 'Draft' ELSE 'Published' END,
              published_at, concurrency_token, created_at, updated_at, tags, content_type,
              bookmark_url, bookmark_title, bookmark_description,
              note_value, post_title, post_excerpt, post_body
            FROM content;

            INSERT INTO content_revision_attachment (
              content_revision_id, content_attachment_id, visibility)
            SELECT content_id, content_attachment_id, visibility
            FROM content_attachment;

            UPDATE content
            SET current_published_revision_id = content_id
            WHERE published_at IS NOT NULL;
            """);

        migrationBuilder.DropColumn(
            name: "visibility",
            table: "content_attachment");

        migrationBuilder.DropColumn(
            name: "bookmark_description",
            table: "content");

        migrationBuilder.DropColumn(
            name: "bookmark_title",
            table: "content");

        migrationBuilder.DropColumn(
            name: "bookmark_url",
            table: "content");

        migrationBuilder.DropColumn(
            name: "note_value",
            table: "content");

        migrationBuilder.DropColumn(
            name: "post_body",
            table: "content");

        migrationBuilder.DropColumn(
            name: "post_excerpt",
            table: "content");

        migrationBuilder.DropColumn(
            name: "post_title",
            table: "content");

        migrationBuilder.DropColumn(
            name: "published_at",
            table: "content");

        migrationBuilder.DropColumn(
            name: "tags",
            table: "content");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "visibility",
            table: "content_attachment",
            type: "TEXT",
            nullable: false,
            defaultValue: "Owner");

        migrationBuilder.AddColumn<string>(
            name: "bookmark_description",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "bookmark_title",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "bookmark_url",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "note_value",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "post_body",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "post_excerpt",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "post_title",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "published_at",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "tags",
            table: "content",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.Sql(
            """
            UPDATE content
            SET
              tags = COALESCE((
                SELECT revision.tags
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ), '[]'),
              bookmark_url = (
                SELECT revision.bookmark_url
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              bookmark_title = (
                SELECT revision.bookmark_title
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              bookmark_description = (
                SELECT revision.bookmark_description
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              note_value = (
                SELECT revision.note_value
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              post_title = (
                SELECT revision.post_title
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              post_excerpt = (
                SELECT revision.post_excerpt
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              post_body = (
                SELECT revision.post_body
                FROM content_revision AS revision
                WHERE revision.content_id = content.content_id
                ORDER BY
                  CASE
                    WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                    WHEN revision.revision_state = 'Draft' THEN 1
                    ELSE 2
                  END,
                  revision.revision_number DESC
                LIMIT 1
              ),
              published_at = (
                SELECT revision.published_at
                FROM content_revision AS revision
                WHERE revision.content_revision_id = content.current_published_revision_id
              );

            UPDATE content_attachment
            SET visibility = COALESCE((
              SELECT revision_attachment.visibility
              FROM content AS content
              JOIN content_revision AS revision
                ON revision.content_id = content.content_id
              JOIN content_revision_attachment AS revision_attachment
                ON revision_attachment.content_revision_id = revision.content_revision_id
                AND revision_attachment.content_attachment_id = content_attachment.content_attachment_id
              WHERE content.content_id = content_attachment.content_id
              ORDER BY
                CASE
                  WHEN revision.content_revision_id = content.current_published_revision_id THEN 0
                  WHEN revision.revision_state = 'Draft' THEN 1
                  ELSE 2
                END,
                revision.revision_number DESC
              LIMIT 1
            ), 'Owner');
            """);

        migrationBuilder.Sql(
            "UPDATE content SET current_published_revision_id = NULL;");

        migrationBuilder.DropForeignKey(
            name: "FK_content_content_revision_current_published_revision_id",
            table: "content");

        migrationBuilder.DropIndex(
            name: "IX_content_current_published_revision_id",
            table: "content");

        migrationBuilder.DropColumn(
            name: "current_published_revision_id",
            table: "content");

        migrationBuilder.DropTable(
            name: "content_revision_attachment");

        migrationBuilder.DropTable(
            name: "content_revision");
    }
}
