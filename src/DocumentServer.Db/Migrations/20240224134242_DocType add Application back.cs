using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class DocTypeaddApplicationback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_RootObjects_RootObjectId",
                table: "DocumentTypes");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationId",
                table: "DocumentTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ApplicationId",
                table: "DocumentTypes",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_Applications_ApplicationId",
                table: "DocumentTypes",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_RootObjects_RootObjectId",
                table: "DocumentTypes",
                column: "RootObjectId",
                principalTable: "RootObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_Applications_ApplicationId",
                table: "DocumentTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_RootObjects_RootObjectId",
                table: "DocumentTypes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTypes_ApplicationId",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "DocumentTypes");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_RootObjects_RootObjectId",
                table: "DocumentTypes",
                column: "RootObjectId",
                principalTable: "RootObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
