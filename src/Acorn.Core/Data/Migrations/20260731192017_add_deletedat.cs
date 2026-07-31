using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class add_deletedat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "published_at",
                table: "content",
                newName: "deleted_at");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "content",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "content");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "content",
                newName: "published_at");
        }
    }
}
