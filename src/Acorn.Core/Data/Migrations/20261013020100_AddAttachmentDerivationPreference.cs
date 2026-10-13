using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations;

/// <inheritdoc />
public partial class _20261013020100_AddAttachmentDerivationPreference : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_preferred",
            table: "content_attachment",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_content_attachment_parent_id",
            table: "content_attachment",
            column: "parent_id",
            unique: true,
            filter: "is_preferred = 1");

        migrationBuilder.AddCheckConstraint(
            name: "ck_content_attachment_preferred_is_public",
            table: "content_attachment",
            sql: "is_preferred = 0 OR visibility = 'Public'");

        migrationBuilder.AddCheckConstraint(
            name: "ck_content_attachment_root_not_preferred",
            table: "content_attachment",
            sql: "parent_id IS NOT NULL OR is_preferred = 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_content_attachment_parent_id",
            table: "content_attachment");

        migrationBuilder.DropCheckConstraint(
            name: "ck_content_attachment_preferred_is_public",
            table: "content_attachment");

        migrationBuilder.DropCheckConstraint(
            name: "ck_content_attachment_root_not_preferred",
            table: "content_attachment");

        migrationBuilder.DropColumn(
            name: "is_preferred",
            table: "content_attachment");
    }
}
