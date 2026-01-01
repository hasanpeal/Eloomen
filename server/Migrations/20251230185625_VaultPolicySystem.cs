using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class VaultPolicySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove SentAt column
            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "VaultInvites");

            // Remove InviteType column
            migrationBuilder.DropColumn(
                name: "InviteType",
                table: "VaultInvites");

            // Add PolicyType column
            migrationBuilder.AddColumn<int>(
                name: "PolicyType",
                table: "VaultInvites",
                type: "integer",
                nullable: false,
                defaultValue: 0); // Default to Immediate

            // Add policy configuration columns
            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "VaultInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyExpiresAt",
                table: "VaultInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PulseIntervalTicks",
                table: "VaultInvites",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VaultPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaultMemberId = table.Column<int>(type: "integer", nullable: false),
                    PolicyType = table.Column<int>(type: "integer", nullable: false),
                    ReleaseStatus = table.Column<int>(type: "integer", nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPulseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PulseIntervalTicks = table.Column<long>(type: "bigint", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultPolicies_Users_ReleasedById",
                        column: x => x.ReleasedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaultPolicies_VaultMembers_VaultMemberId",
                        column: x => x.VaultMemberId,
                        principalTable: "VaultMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaultPolicies_ExpiresAt",
                table: "VaultPolicies",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_VaultPolicies_PolicyType",
                table: "VaultPolicies",
                column: "PolicyType");

            migrationBuilder.CreateIndex(
                name: "IX_VaultPolicies_ReleaseDate",
                table: "VaultPolicies",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_VaultPolicies_ReleasedById",
                table: "VaultPolicies",
                column: "ReleasedById");

            migrationBuilder.CreateIndex(
                name: "IX_VaultPolicies_ReleaseStatus",
                table: "VaultPolicies",
                column: "ReleaseStatus");

            migrationBuilder.CreateIndex(
                name: "IX_VaultPolicies_VaultMemberId",
                table: "VaultPolicies",
                column: "VaultMemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaultPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyType",
                table: "VaultInvites");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "VaultInvites");

            migrationBuilder.DropColumn(
                name: "PolicyExpiresAt",
                table: "VaultInvites");

            migrationBuilder.DropColumn(
                name: "PulseIntervalTicks",
                table: "VaultInvites");

            // Restore SentAt column
            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "VaultInvites",
                type: "timestamp with time zone",
                nullable: true);

            // Restore InviteType column
            migrationBuilder.AddColumn<int>(
                name: "InviteType",
                table: "VaultInvites",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
