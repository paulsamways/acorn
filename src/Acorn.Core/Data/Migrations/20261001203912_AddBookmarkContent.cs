using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookmarkContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bookmark_description",
                table: "content");

            migrationBuilder.DropColumn(
                name: "bookmark_title",
                table: "content");

            migrationBuilder.DropColumn(
                name: "bookmark_url",
                table: "content");
        }
    }
}
