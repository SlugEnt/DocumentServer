using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                                         name: "VitalInfos",
                                         columns: table => new
                                         {
                                             Id            = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                                             Name          = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                                             ValueLong     = table.Column<long>(type: "bigint", nullable: false),
                                             ValueString   = table.Column<string>(type: "nvarchar(max)", nullable: false),
                                             LastUpdateUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                                         },
                                         constraints: table => { table.PrimaryKey("PK_VitalInfos", x => x.Id); });

            migrationBuilder.InsertData(
                table: "VitalInfos",
                columns: new[] { "Id", "LastUpdateUtc", "Name", "ValueLong", "ValueString" },
                values: new object[] { "LastKeyEntityUpdate", new DateTime(1, 1, 1, 0, 0, 0, 1, DateTimeKind.Unspecified), "Last Update to Key Entities", 0L, "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VitalInfos",
                keyColumn: "Id",
                keyValue: "LastKeyEntityUpdate");

            migrationBuilder.DropTable(
                                       name: "VitalInfos");
        }
    }
}
