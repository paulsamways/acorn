using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Acorn.Core.Database.Migrations;

/// <inheritdoc />
public partial class _20261019203527_AddContentConcurrencyToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "concurrency_token",
            table: "content",
            type: "BLOB",
            nullable: false,
            defaultValue: new byte[16]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "concurrency_token",
            table: "content");
    }
}
