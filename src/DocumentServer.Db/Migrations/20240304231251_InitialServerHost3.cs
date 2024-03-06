using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class InitialServerHost3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpiringDocuments",
                columns: table => new
                {
                    StoredDocumentId = table.Column<long>(type: "bigint", nullable: false),
                    ExpirationDateUtcDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpiringDocuments", x => x.StoredDocumentId);
                });

            migrationBuilder.CreateTable(
                name: "ServerHosts",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameDNS = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerHosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RootObjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "StorageNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTestNode = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NodePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ServerHostId = table.Column<short>(type: "smallint", nullable: false),
                    StorageNodeLocation = table.Column<byte>(type: "tinyint", nullable: false),
                    StorageSpeed = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageNodes_ServerHosts_ServerHostId",
                        column: x => x.ServerHostId,
                        principalTable: "ServerHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActiveStorageNode1Id = table.Column<int>(type: "int", nullable: true),
                    ActiveStorageNode2Id = table.Column<int>(type: "int", nullable: true),
                    ArchivalStorageNode1Id = table.Column<int>(type: "int", nullable: true),
                    ArchivalStorageNode2Id = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    InActiveLifeTime = table.Column<byte>(type: "tinyint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(75)", maxLength: 75, nullable: false),
                    StorageFolderName = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StorageMode = table.Column<byte>(type: "tinyint", nullable: false),
                    RootObjectId = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    AllowSameDTEKeys = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_RootObjects_RootObjectId",
                        column: x => x.RootObjectId,
                        principalTable: "RootObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_StorageNodes_ActiveStorageNode1Id",
                        column: x => x.ActiveStorageNode1Id,
                        principalTable: "StorageNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentTypes_StorageNodes_ActiveStorageNode2Id",
                        column: x => x.ActiveStorageNode2Id,
                        principalTable: "StorageNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentTypes_StorageNodes_ArchivalStorageNode1Id",
                        column: x => x.ArchivalStorageNode1Id,
                        principalTable: "StorageNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DocumentTypes_StorageNodes_ArchivalStorageNode2Id",
                        column: x => x.ArchivalStorageNode2Id,
                        principalTable: "StorageNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoredDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DocTypeExternalKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAlive = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    LastAccessedUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    NumberOfTimesAccessed = table.Column<int>(type: "int", nullable: false),
                    PrimaryStorageNodeId = table.Column<int>(type: "int", nullable: true),
                    RootObjectExternalKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SecondaryStorageNodeId = table.Column<int>(type: "int", nullable: true),
                    SizeInKB = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StorageFolder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoredDocuments_StorageNodes_PrimaryStorageNodeId",
                        column: x => x.PrimaryStorageNodeId,
                        principalTable: "StorageNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StoredDocuments_StorageNodes_SecondaryStorageNodeId",
                        column: x => x.SecondaryStorageNodeId,
                        principalTable: "StorageNodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ActiveStorageNode1Id",
                table: "DocumentTypes",
                column: "ActiveStorageNode1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ActiveStorageNode2Id",
                table: "DocumentTypes",
                column: "ActiveStorageNode2Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ApplicationId",
                table: "DocumentTypes",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ArchivalStorageNode1Id",
                table: "DocumentTypes",
                column: "ArchivalStorageNode1Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ArchivalStorageNode2Id",
                table: "DocumentTypes",
                column: "ArchivalStorageNode2Id");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_RootObjectId",
                table: "DocumentTypes",
                column: "RootObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RootObjects_ApplicationId",
                table: "RootObjects",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageNodes_ServerHostId",
                table: "StorageNodes",
                column: "ServerHostId");

            migrationBuilder.CreateIndex(
                name: "IDX_Ext_Keys",
                table: "StoredDocuments",
                columns: new[] { "RootObjectExternalKey", "DocTypeExternalKey" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_DocumentTypeId",
                table: "StoredDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_PrimaryStorageNodeId",
                table: "StoredDocuments",
                column: "PrimaryStorageNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredDocuments_SecondaryStorageNodeId",
                table: "StoredDocuments",
                column: "SecondaryStorageNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpiringDocuments");

            migrationBuilder.DropTable(
                name: "StoredDocuments");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "RootObjects");

            migrationBuilder.DropTable(
                name: "StorageNodes");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "ServerHosts");
        }
    }
}
