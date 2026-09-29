using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "body",
                table: "content",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tags",
                table: "content",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "content",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "body",
                table: "content");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "content");

            migrationBuilder.DropColumn(
                name: "title",
                table: "content");
        }
    }
}
