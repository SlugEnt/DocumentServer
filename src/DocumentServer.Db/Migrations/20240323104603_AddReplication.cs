using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlugEnt.DocumentServer.Db.Migrations
{
    /// <inheritdoc />
    public partial class AddReplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NodePath",
                table: "StorageNodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "ReplicationTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoredDocumentId = table.Column<long>(type: "bigint", nullable: false),
                    ReplicateToStorageNodeId = table.Column<int>(type: "int", nullable: false),
                    ReplicateFromStorageNodeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplicationTasks_StorageNodes_ReplicateFromStorageNodeId",
                        column: x => x.ReplicateFromStorageNodeId,
                        principalTable: "StorageNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplicationTasks_StorageNodes_ReplicateToStorageNodeId",
                        column: x => x.ReplicateToStorageNodeId,
                        principalTable: "StorageNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplicationTasks_StoredDocuments_StoredDocumentId",
                        column: x => x.StoredDocumentId,
                        principalTable: "StoredDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReplicationTasks_ReplicateFromStorageNodeId",
                table: "ReplicationTasks",
                column: "ReplicateFromStorageNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplicationTasks_ReplicateToStorageNodeId",
                table: "ReplicationTasks",
                column: "ReplicateToStorageNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplicationTasks_StoredDocumentId",
                table: "ReplicationTasks",
                column: "StoredDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReplicationTasks");

            migrationBuilder.AlterColumn<string>(
                name: "NodePath",
                table: "StorageNodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
