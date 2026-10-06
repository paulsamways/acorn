using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations;

/// <inheritdoc />
public partial class _20261006045256_AddFileStorageMetadata : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "public_id",
            table: "content",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "content"
            SET "public_id" =
                lower(hex(randomblob(4))) || '-' ||
                lower(hex(randomblob(2))) || '-4' ||
                substr(lower(hex(randomblob(2))), 2) || '-' ||
                substr('89ab', (random() & 3) + 1, 1) ||
                substr(lower(hex(randomblob(2))), 2) || '-' ||
                lower(hex(randomblob(6)));
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "public_id",
            table: "content",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "blob",
            columns: table => new
            {
                digest = table.Column<byte[]>(type: "BLOB", nullable: false),
                size = table.Column<long>(type: "INTEGER", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blob", x => x.digest);
                table.CheckConstraint("ck_blob_digest_length", "length(digest) = 32");
                table.CheckConstraint("ck_blob_size", "size >= 0");
            });

        migrationBuilder.CreateTable(
            name: "file",
            columns: table => new
            {
                file_id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                public_id = table.Column<Guid>(type: "TEXT", nullable: false),
                blob_digest = table.Column<byte[]>(type: "BLOB", nullable: false),
                owner_id = table.Column<Guid>(type: "TEXT", nullable: false),
                original_filename = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                declared_media_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                validated_media_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                parent_file_id = table.Column<int>(type: "INTEGER", nullable: true),
                recipe = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                recipe_version = table.Column<int>(type: "INTEGER", nullable: true),
                file_type = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                image_width = table.Column<int>(type: "INTEGER", nullable: true),
                image_height = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_file", x => x.file_id);
                table.CheckConstraint("ck_file_derivation", "(parent_file_id IS NULL AND recipe IS NULL AND recipe_version IS NULL) OR (parent_file_id IS NOT NULL AND recipe IS NOT NULL AND recipe_version > 0)");
                table.ForeignKey(
                    name: "FK_file_blob_blob_digest",
                    column: x => x.blob_digest,
                    principalTable: "blob",
                    principalColumn: "digest",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_file_file_parent_file_id",
                    column: x => x.parent_file_id,
                    principalTable: "file",
                    principalColumn: "file_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_file_user_owner_id",
                    column: x => x.owner_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "content_attachment",
            columns: table => new
            {
                content_id = table.Column<int>(type: "INTEGER", nullable: false),
                file_id = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_content_attachment", x => new { x.content_id, x.file_id });
                table.ForeignKey(
                    name: "FK_content_attachment_content_content_id",
                    column: x => x.content_id,
                    principalTable: "content",
                    principalColumn: "content_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_content_attachment_file_file_id",
                    column: x => x.file_id,
                    principalTable: "file",
                    principalColumn: "file_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_content_public_id",
            table: "content",
            column: "public_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_content_attachment_file_id",
            table: "content_attachment",
            column: "file_id");

        migrationBuilder.CreateIndex(
            name: "IX_file_blob_digest",
            table: "file",
            column: "blob_digest");

        migrationBuilder.CreateIndex(
            name: "IX_file_owner_id_deleted_at",
            table: "file",
            columns: new[] { "owner_id", "deleted_at" });

        migrationBuilder.CreateIndex(
            name: "IX_file_parent_file_id_recipe",
            table: "file",
            columns: new[] { "parent_file_id", "recipe" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_file_public_id",
            table: "file",
            column: "public_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "content_attachment");

        migrationBuilder.DropTable(
            name: "file");

        migrationBuilder.DropTable(
            name: "blob");

        migrationBuilder.DropIndex(
            name: "IX_content_public_id",
            table: "content");

        migrationBuilder.DropColumn(
            name: "public_id",
            table: "content");
    }
}
