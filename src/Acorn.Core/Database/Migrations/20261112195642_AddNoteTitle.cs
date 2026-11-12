using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Database.Migrations;

/// <inheritdoc />
public partial class _20261112195642_AddNoteTitle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_content_revision_required_type_fields",
            table: "content_revision");

        migrationBuilder.AddColumn<string>(
            name: "note_title",
            table: "content_revision",
            type: "TEXT",
            nullable: true,
            defaultValue: "");

        migrationBuilder.AddCheckConstraint(
            name: "ck_content_revision_required_type_fields",
            table: "content_revision",
            sql: "(revision_type = 'post' AND post_title IS NOT NULL AND post_body IS NOT NULL) OR (revision_type = 'bookmark' AND bookmark_url IS NOT NULL AND bookmark_title IS NOT NULL) OR (revision_type = 'note' AND note_title IS NOT NULL AND note_value IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_content_revision_required_type_fields",
            table: "content_revision");

        migrationBuilder.DropColumn(
            name: "note_title",
            table: "content_revision");

        migrationBuilder.AddCheckConstraint(
            name: "ck_content_revision_required_type_fields",
            table: "content_revision",
            sql: "(revision_type = 'post' AND post_title IS NOT NULL AND post_body IS NOT NULL) OR (revision_type = 'bookmark' AND bookmark_url IS NOT NULL AND bookmark_title IS NOT NULL) OR (revision_type = 'note' AND note_value IS NOT NULL)");
    }
}
