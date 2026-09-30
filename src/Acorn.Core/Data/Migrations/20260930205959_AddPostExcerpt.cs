using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostExcerpt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "excerpt",
                table: "content",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "excerpt",
                table: "content");
        }
    }
}
