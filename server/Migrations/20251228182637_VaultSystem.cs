using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class VaultSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vaults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    OriginalOwnerId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vaults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vaults_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaultInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaultId = table.Column<int>(type: "integer", nullable: false),
                    InviterId = table.Column<string>(type: "text", nullable: false),
                    InviteeEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InviteeId = table.Column<string>(type: "text", nullable: true),
                    Privilege = table.Column<int>(type: "integer", nullable: false),
                    InviteType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultInvites_Users_InviteeId",
                        column: x => x.InviteeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaultInvites_Users_InviterId",
                        column: x => x.InviterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaultInvites_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaultId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Privilege = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RemovedById = table.Column<string>(type: "text", nullable: true),
                    AddedById = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultMembers_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaultMembers_Users_RemovedById",
                        column: x => x.RemovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaultMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaultMembers_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_ExpiresAt",
                table: "VaultInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_InviteeEmail",
                table: "VaultInvites",
                column: "InviteeEmail");

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_InviteeId",
                table: "VaultInvites",
                column: "InviteeId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_InviterId",
                table: "VaultInvites",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_Status",
                table: "VaultInvites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_TokenHash",
                table: "VaultInvites",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultInvites_VaultId",
                table: "VaultInvites",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMembers_AddedById",
                table: "VaultMembers",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMembers_RemovedById",
                table: "VaultMembers",
                column: "RemovedById");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMembers_Status",
                table: "VaultMembers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMembers_UserId",
                table: "VaultMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMembers_VaultId",
                table: "VaultMembers",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultMembers_VaultId_UserId_Status",
                table: "VaultMembers",
                columns: new[] { "VaultId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Vaults_DeletedAt",
                table: "Vaults",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Vaults_OwnerId",
                table: "Vaults",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vaults_Status",
                table: "Vaults",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaultInvites");

            migrationBuilder.DropTable(
                name: "VaultMembers");

            migrationBuilder.DropTable(
                name: "Vaults");
        }
    }
}
