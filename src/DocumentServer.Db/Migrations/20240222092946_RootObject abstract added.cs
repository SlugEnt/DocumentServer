using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class RootObjectabstractadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUTC",
                table: "RootObjects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RootObjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "RootObjects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUTC",
                table: "RootObjects");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RootObjects");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUTC",
                table: "RootObjects");
        }
    }
}
