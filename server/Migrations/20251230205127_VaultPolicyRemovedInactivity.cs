using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class VaultPolicyRemovedInactivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPulseDate",
                table: "VaultPolicies");

            migrationBuilder.DropColumn(
                name: "PulseIntervalTicks",
                table: "VaultPolicies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPulseDate",
                table: "VaultPolicies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PulseIntervalTicks",
                table: "VaultPolicies",
                type: "bigint",
                nullable: true);
        }
    }
}
