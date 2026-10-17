using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Database.Migrations;

/// <inheritdoc />
public partial class _20261016210311_InitialUuidSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            name: "data_protection_keys",
            columns: table => new
            {
                data_protection_keys_id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                friendly_name = table.Column<string>(type: "TEXT", nullable: true),
                xml = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_data_protection_keys", x => x.data_protection_keys_id);
            });

        migrationBuilder.CreateTable(
            name: "role",
            columns: table => new
            {
                role_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                normalized_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                concurrency_stamp = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role", x => x.role_id);
            });

        migrationBuilder.CreateTable(
            name: "user",
            columns: table => new
            {
                user_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                time_zone = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                user_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                normalized_user_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                normalized_email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                email_confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                password_hash = table.Column<string>(type: "TEXT", nullable: true),
                security_stamp = table.Column<string>(type: "TEXT", nullable: true),
                concurrency_stamp = table.Column<string>(type: "TEXT", nullable: true),
                phone_number = table.Column<string>(type: "TEXT", nullable: true),
                phone_number_confirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                two_factor_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                lockout_end = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                lockout_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                access_failed_count = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user", x => x.user_id);
            });

        migrationBuilder.CreateTable(
            name: "role_claim",
            columns: table => new
            {
                role_claim_id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                role_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                claim_type = table.Column<string>(type: "TEXT", nullable: true),
                claim_value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_claim", x => x.role_claim_id);
                table.ForeignKey(
                    name: "FK_role_claim_role_role_id",
                    column: x => x.role_id,
                    principalTable: "role",
                    principalColumn: "role_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "content",
            columns: table => new
            {
                content_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                published_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                author_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                tags = table.Column<string>(type: "TEXT", nullable: false),
                content_type = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
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
                table.PrimaryKey("PK_content", x => x.content_id);
                table.ForeignKey(
                    name: "FK_content_user_author_id",
                    column: x => x.author_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "file",
            columns: table => new
            {
                file_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                blob_digest = table.Column<byte[]>(type: "BLOB", nullable: false),
                owner_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                filename = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                media_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                file_type = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                image_width = table.Column<int>(type: "INTEGER", nullable: true),
                image_height = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_file", x => x.file_id);
                table.ForeignKey(
                    name: "FK_file_blob_blob_digest",
                    column: x => x.blob_digest,
                    principalTable: "blob",
                    principalColumn: "digest",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_file_user_owner_id",
                    column: x => x.owner_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_claim",
            columns: table => new
            {
                user_claim_id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                user_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                claim_type = table.Column<string>(type: "TEXT", nullable: true),
                claim_value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_claim", x => x.user_claim_id);
                table.ForeignKey(
                    name: "FK_user_claim_user_user_id",
                    column: x => x.user_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_login",
            columns: table => new
            {
                login_provider = table.Column<string>(type: "TEXT", nullable: false),
                provider_key = table.Column<string>(type: "TEXT", nullable: false),
                provider_display_name = table.Column<string>(type: "TEXT", nullable: true),
                user_id = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_login", x => new { x.login_provider, x.provider_key });
                table.ForeignKey(
                    name: "FK_user_login_user_user_id",
                    column: x => x.user_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_role",
            columns: table => new
            {
                user_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                role_id = table.Column<byte[]>(type: "BLOB", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_role", x => new { x.user_id, x.role_id });
                table.ForeignKey(
                    name: "FK_user_role_role_role_id",
                    column: x => x.role_id,
                    principalTable: "role",
                    principalColumn: "role_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_user_role_user_user_id",
                    column: x => x.user_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_token",
            columns: table => new
            {
                user_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                login_provider = table.Column<string>(type: "TEXT", nullable: false),
                name = table.Column<string>(type: "TEXT", nullable: false),
                value = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_token", x => new { x.user_id, x.login_provider, x.name });
                table.ForeignKey(
                    name: "FK_user_token_user_user_id",
                    column: x => x.user_id,
                    principalTable: "user",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "content_attachment",
            columns: table => new
            {
                content_attachment_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                content_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                file_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                visibility = table.Column<string>(type: "TEXT", nullable: false),
                parent_id = table.Column<byte[]>(type: "BLOB", nullable: true),
                derivation_name = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_content_attachment", x => x.content_attachment_id);
                table.ForeignKey(
                    name: "FK_content_attachment_content_attachment_parent_id",
                    column: x => x.parent_id,
                    principalTable: "content_attachment",
                    principalColumn: "content_attachment_id",
                    onDelete: ReferentialAction.Cascade);
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
            name: "IX_content_author_id",
            table: "content",
            column: "author_id");

        migrationBuilder.CreateIndex(
            name: "IX_content_attachment_content_id",
            table: "content_attachment",
            column: "content_id");

        migrationBuilder.CreateIndex(
            name: "IX_content_attachment_file_id",
            table: "content_attachment",
            column: "file_id");

        migrationBuilder.CreateIndex(
            name: "IX_content_attachment_parent_id_derivation_name",
            table: "content_attachment",
            columns: new[] { "parent_id", "derivation_name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_file_blob_digest",
            table: "file",
            column: "blob_digest");

        migrationBuilder.CreateIndex(
            name: "IX_file_owner_id_deleted_at",
            table: "file",
            columns: new[] { "owner_id", "deleted_at" });

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "role",
            column: "normalized_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_claim_role_id",
            table: "role_claim",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "user",
            column: "normalized_email");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "user",
            column: "normalized_user_name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_user_claim_user_id",
            table: "user_claim",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_login_user_id",
            table: "user_login",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_user_role_role_id",
            table: "user_role",
            column: "role_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "content_attachment");

        migrationBuilder.DropTable(
            name: "data_protection_keys");

        migrationBuilder.DropTable(
            name: "role_claim");

        migrationBuilder.DropTable(
            name: "user_claim");

        migrationBuilder.DropTable(
            name: "user_login");

        migrationBuilder.DropTable(
            name: "user_role");

        migrationBuilder.DropTable(
            name: "user_token");

        migrationBuilder.DropTable(
            name: "content");

        migrationBuilder.DropTable(
            name: "file");

        migrationBuilder.DropTable(
            name: "role");

        migrationBuilder.DropTable(
            name: "blob");

        migrationBuilder.DropTable(
            name: "user");
    }
}
