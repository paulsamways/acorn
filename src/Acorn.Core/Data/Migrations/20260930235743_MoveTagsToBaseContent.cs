using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveTagsToBaseContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "post_tags",
                table: "content",
                newName: "tags");

            migrationBuilder.AlterColumn<string>(
                name: "tags",
                table: "content",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "tags",
                table: "content",
                newName: "post_tags");

            migrationBuilder.AlterColumn<string>(
                name: "post_tags",
                table: "content",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
