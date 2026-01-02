using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsAndVaultReleaseTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VaultId = table.Column<int>(type: "integer", nullable: true),
                    ItemId = table.Column<int>(type: "integer", nullable: true),
                    InviteId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_VaultId",
                table: "Notifications",
                column: "VaultId");

            // Create PostgreSQL function to handle vault release notifications
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION notify_vault_released()
                RETURNS TRIGGER AS $$
                DECLARE
                    vault_name TEXT;
                    member_record RECORD;
                BEGIN
                    -- Only trigger when ReleaseStatus changes from non-Released to Released
                    -- ReleaseStatus values: 0=Pending, 1=Released, 2=Expired, 3=Revoked
                    IF OLD.""ReleaseStatus"" != 1 AND NEW.""ReleaseStatus"" = 1 THEN
                        -- Get vault name
                        SELECT ""Name"" INTO vault_name
                        FROM ""Vaults""
                        WHERE ""Id"" = NEW.""VaultId"";

                        -- Create notifications for all active vault members
                        FOR member_record IN
                            SELECT ""UserId""
                            FROM ""VaultMembers""
                            WHERE ""VaultId"" = NEW.""VaultId""
                            AND ""Status"" = 0  -- MemberStatus.Active = 0
                        LOOP
                            INSERT INTO ""Notifications"" (""UserId"", ""Title"", ""Description"", ""Type"", ""IsRead"", ""CreatedAt"", ""VaultId"")
                            VALUES (
                                member_record.""UserId"",
                                'Vault Released',
                                'The vault ''' || COALESCE(vault_name, 'Unknown') || ''' has been released and is now accessible.',
                                'VaultReleased',
                                false,
                                NOW(),
                                NEW.""VaultId""
                            );
                        END LOOP;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create trigger on VaultPolicies table
            migrationBuilder.Sql(@"
                CREATE TRIGGER vault_release_notification_trigger
                AFTER UPDATE OF ""ReleaseStatus"" ON ""VaultPolicies""
                FOR EACH ROW
                EXECUTE FUNCTION notify_vault_released();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigger and function
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS vault_release_notification_trigger ON ""VaultPolicies"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS notify_vault_released();");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
