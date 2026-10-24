using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Database.Migrations;

/// <inheritdoc />
public partial class _20261024194916_AddContentComments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "content_comment",
            columns: table => new
            {
                content_comment_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                concurrency_token = table.Column<byte[]>(type: "BLOB", nullable: false),
                created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                content_id = table.Column<byte[]>(type: "BLOB", nullable: false),
                parent_comment_id = table.Column<byte[]>(type: "BLOB", nullable: true),
                depth = table.Column<int>(type: "INTEGER", nullable: false),
                author_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                author_email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                body = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                comment_state = table.Column<string>(type: "TEXT", nullable: false),
                verification_token_hash = table.Column<byte[]>(type: "BLOB", nullable: true),
                verification_token_expires_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                verification_sent_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                verified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                approved_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                hidden_at = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_content_comment", x => x.content_comment_id);
                table.UniqueConstraint("AK_content_comment_content_id_content_comment_id", x => new { x.content_id, x.content_comment_id });
                table.CheckConstraint("ck_content_comment_depth", "depth >= 0");
                table.CheckConstraint("ck_content_comment_state", "comment_state IN ('Unverified', 'Verified', 'Approved', 'Hidden')");
                table.ForeignKey(
                    name: "FK_content_comment_content_comment_content_id_parent_comment_id",
                    columns: x => new { x.content_id, x.parent_comment_id },
                    principalTable: "content_comment",
                    principalColumns: new[] { "content_id", "content_comment_id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_content_comment_content_content_id",
                    column: x => x.content_id,
                    principalTable: "content",
                    principalColumn: "content_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_content_comment_comment_state_created_at",
            table: "content_comment",
            columns: new[] { "comment_state", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_content_comment_content_id_comment_state_created_at",
            table: "content_comment",
            columns: new[] { "content_id", "comment_state", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_content_comment_content_id_parent_comment_id",
            table: "content_comment",
            columns: new[] { "content_id", "parent_comment_id" });

        migrationBuilder.CreateIndex(
            name: "IX_content_comment_verification_token_hash",
            table: "content_comment",
            column: "verification_token_hash",
            unique: true,
            filter: "\"verification_token_hash\" IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "content_comment");
    }
}
