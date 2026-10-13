using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations;

/// <inheritdoc />
public partial class _20261013023806_RenameAttachmentPreferredDerivation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
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

        migrationBuilder.RenameColumn(
            name: "is_preferred",
            table: "content_attachment",
            newName: "is_preferred_derivation");

        migrationBuilder.AlterColumn<bool>(
            name: "is_preferred_derivation",
            table: "content_attachment",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(bool),
            oldType: "INTEGER",
            oldDefaultValue: false);

        migrationBuilder.Sql(
            "UPDATE content_attachment SET is_preferred_derivation = NULL WHERE parent_id IS NULL;");

        migrationBuilder.CreateIndex(
            name: "IX_content_attachment_parent_id",
            table: "content_attachment",
            column: "parent_id",
            unique: true,
            filter: "is_preferred_derivation = 1");

        migrationBuilder.AddCheckConstraint(
            name: "ck_content_attachment_preferred_derivation_is_public",
            table: "content_attachment",
            sql: "is_preferred_derivation = 0 OR visibility = 'Public'");

        migrationBuilder.AddCheckConstraint(
            name: "ck_content_attachment_preferred_derivation_requires_parent",
            table: "content_attachment",
            sql: "(parent_id IS NULL AND is_preferred_derivation IS NULL) OR (parent_id IS NOT NULL AND is_preferred_derivation IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_content_attachment_parent_id",
            table: "content_attachment");

        migrationBuilder.DropCheckConstraint(
            name: "ck_content_attachment_preferred_derivation_is_public",
            table: "content_attachment");

        migrationBuilder.DropCheckConstraint(
            name: "ck_content_attachment_preferred_derivation_requires_parent",
            table: "content_attachment");

        migrationBuilder.Sql(
            "UPDATE content_attachment SET is_preferred_derivation = 0 WHERE parent_id IS NULL;");

        migrationBuilder.RenameColumn(
            name: "is_preferred_derivation",
            table: "content_attachment",
            newName: "is_preferred");

        migrationBuilder.AlterColumn<bool>(
            name: "is_preferred",
            table: "content_attachment",
            type: "INTEGER",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "INTEGER",
            oldNullable: true);

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
}
