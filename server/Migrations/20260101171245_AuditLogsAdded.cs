using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdditionalContext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaultLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaultId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetUserId = table.Column<string>(type: "text", nullable: true),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    AdditionalContext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaultLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaultLogs_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogs_Action",
                table: "AccountLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogs_Timestamp",
                table: "AccountLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLogs_UserId",
                table: "AccountLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultLogs_Action",
                table: "VaultLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_VaultLogs_ItemId",
                table: "VaultLogs",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultLogs_TargetUserId",
                table: "VaultLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultLogs_Timestamp",
                table: "VaultLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_VaultLogs_UserId",
                table: "VaultLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultLogs_VaultId",
                table: "VaultLogs",
                column: "VaultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountLogs");

            migrationBuilder.DropTable(
                name: "VaultLogs");
        }
    }
}
