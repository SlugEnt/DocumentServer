using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class ReplicationcreatedModifieddates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ReplicationTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUTC",
                table: "ReplicationTasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ReplicationTasks");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUTC",
                table: "ReplicationTasks");
        }
    }
}
