using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    content_type = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_user_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_AuthorId",
                table: "content",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_content_Slug",
                table: "content",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content");
        }
    }
}
