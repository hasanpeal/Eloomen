using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class VaultPolicyUpdte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaultPolicies_VaultMembers_VaultMemberId",
                table: "VaultPolicies");

            migrationBuilder.DropColumn(
                name: "PolicyExpiresAt",
                table: "VaultInvites");

            migrationBuilder.DropColumn(
                name: "PolicyType",
                table: "VaultInvites");

            migrationBuilder.DropColumn(
                name: "PulseIntervalTicks",
                table: "VaultInvites");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "VaultInvites");

            migrationBuilder.RenameColumn(
                name: "VaultMemberId",
                table: "VaultPolicies",
                newName: "VaultId");

            migrationBuilder.RenameIndex(
                name: "IX_VaultPolicies_VaultMemberId",
                table: "VaultPolicies",
                newName: "IX_VaultPolicies_VaultId");

            migrationBuilder.AddForeignKey(
                name: "FK_VaultPolicies_Vaults_VaultId",
                table: "VaultPolicies",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaultPolicies_Vaults_VaultId",
                table: "VaultPolicies");

            migrationBuilder.RenameColumn(
                name: "VaultId",
                table: "VaultPolicies",
                newName: "VaultMemberId");

            migrationBuilder.RenameIndex(
                name: "IX_VaultPolicies_VaultId",
                table: "VaultPolicies",
                newName: "IX_VaultPolicies_VaultMemberId");

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyExpiresAt",
                table: "VaultInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyType",
                table: "VaultInvites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "PulseIntervalTicks",
                table: "VaultInvites",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "VaultInvites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VaultPolicies_VaultMembers_VaultMemberId",
                table: "VaultPolicies",
                column: "VaultMemberId",
                principalTable: "VaultMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
