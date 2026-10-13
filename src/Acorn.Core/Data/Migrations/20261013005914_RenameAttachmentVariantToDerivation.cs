using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations;

/// <inheritdoc />
public partial class _20261013005914_RenameAttachmentVariantToDerivation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "variant_name",
            table: "content_attachment",
            newName: "derivation_name");

        migrationBuilder.RenameIndex(
            name: "IX_content_attachment_parent_id_variant_name",
            table: "content_attachment",
            newName: "IX_content_attachment_parent_id_derivation_name");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "derivation_name",
            table: "content_attachment",
            newName: "variant_name");

        migrationBuilder.RenameIndex(
            name: "IX_content_attachment_parent_id_derivation_name",
            table: "content_attachment",
            newName: "IX_content_attachment_parent_id_variant_name");
    }
}
