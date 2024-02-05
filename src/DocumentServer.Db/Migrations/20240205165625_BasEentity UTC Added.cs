using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class BasEentityUTCAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "StoredDocuments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StoredDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUTC",
                table: "StorageNodes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "StorageNodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "DocumentTypes",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUTC",
                table: "Applications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "Applications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedAtUTC",
                table: "StorageNodes");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUTC",
                table: "StorageNodes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAtUTC",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUTC",
                table: "Applications");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "StoredDocuments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "DocumentTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
