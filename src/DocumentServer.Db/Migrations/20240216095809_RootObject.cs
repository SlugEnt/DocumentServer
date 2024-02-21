using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class RootObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_Applications_ApplicationId",
                table: "DocumentTypes");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "DocumentTypes",
                newName: "RootObjectId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentTypes_ApplicationId",
                table: "DocumentTypes",
                newName: "IX_DocumentTypes_RootObjectId");

            migrationBuilder.AddColumn<string>(
                name: "DocTypeExternalKey",
                table: "StoredDocuments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootObjectExternalKey",
                table: "StoredDocuments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AllowSameDTEKeys",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RootObjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RootObjects_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_Ext_Keys",
                table: "StoredDocuments",
                columns: new[] { "RootObjectExternalKey", "DocTypeExternalKey" });

            migrationBuilder.CreateIndex(
                name: "IX_RootObjects_ApplicationId",
                table: "RootObjects",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_RootObjects_RootObjectId",
                table: "DocumentTypes",
                column: "RootObjectId",
                principalTable: "RootObjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_RootObjects_RootObjectId",
                table: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "RootObjects");

            migrationBuilder.DropIndex(
                name: "IDX_Ext_Keys",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "DocTypeExternalKey",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "RootObjectExternalKey",
                table: "StoredDocuments");

            migrationBuilder.DropColumn(
                name: "AllowSameDTEKeys",
                table: "DocumentTypes");

            migrationBuilder.RenameColumn(
                name: "RootObjectId",
                table: "DocumentTypes",
                newName: "ApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentTypes_RootObjectId",
                table: "DocumentTypes",
                newName: "IX_DocumentTypes_ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_Applications_ApplicationId",
                table: "DocumentTypes",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
