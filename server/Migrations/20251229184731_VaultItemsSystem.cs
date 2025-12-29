using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class VaultItemsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VaultItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaultId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultItems_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaultItems_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VaultItems_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultCryptoWallets",
                columns: table => new
                {
                    VaultItemId = table.Column<int>(type: "integer", nullable: false),
                    WalletType = table.Column<int>(type: "integer", nullable: false),
                    PlatformName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Blockchain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublicAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedSecret = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultCryptoWallets", x => x.VaultItemId);
                    table.ForeignKey(
                        name: "FK_VaultCryptoWallets_VaultItems_VaultItemId",
                        column: x => x.VaultItemId,
                        principalTable: "VaultItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultDocuments",
                columns: table => new
                {
                    VaultItemId = table.Column<int>(type: "integer", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultDocuments", x => x.VaultItemId);
                    table.ForeignKey(
                        name: "FK_VaultDocuments_VaultItems_VaultItemId",
                        column: x => x.VaultItemId,
                        principalTable: "VaultItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultItemVisibilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VaultItemId = table.Column<int>(type: "integer", nullable: false),
                    VaultMemberId = table.Column<int>(type: "integer", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultItemVisibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultItemVisibilities_VaultItems_VaultItemId",
                        column: x => x.VaultItemId,
                        principalTable: "VaultItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultItemVisibilities_VaultMembers_VaultMemberId",
                        column: x => x.VaultMemberId,
                        principalTable: "VaultMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultLinks",
                columns: table => new
                {
                    VaultItemId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultLinks", x => x.VaultItemId);
                    table.ForeignKey(
                        name: "FK_VaultLinks_VaultItems_VaultItemId",
                        column: x => x.VaultItemId,
                        principalTable: "VaultItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultNotes",
                columns: table => new
                {
                    VaultItemId = table.Column<int>(type: "integer", nullable: false),
                    EncryptedContent = table.Column<string>(type: "text", nullable: false),
                    ContentFormat = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultNotes", x => x.VaultItemId);
                    table.ForeignKey(
                        name: "FK_VaultNotes_VaultItems_VaultItemId",
                        column: x => x.VaultItemId,
                        principalTable: "VaultItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultPasswords",
                columns: table => new
                {
                    VaultItemId = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "text", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultPasswords", x => x.VaultItemId);
                    table.ForeignKey(
                        name: "FK_VaultPasswords_VaultItems_VaultItemId",
                        column: x => x.VaultItemId,
                        principalTable: "VaultItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaultDocuments_ObjectKey",
                table: "VaultDocuments",
                column: "ObjectKey");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItems_CreatedByUserId",
                table: "VaultItems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItems_DeletedAt",
                table: "VaultItems",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItems_DeletedBy",
                table: "VaultItems",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItems_ItemType",
                table: "VaultItems",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItems_Status",
                table: "VaultItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItems_VaultId",
                table: "VaultItems",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItemVisibilities_VaultItemId",
                table: "VaultItemVisibilities",
                column: "VaultItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultItemVisibilities_VaultItemId_VaultMemberId",
                table: "VaultItemVisibilities",
                columns: new[] { "VaultItemId", "VaultMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultItemVisibilities_VaultMemberId",
                table: "VaultItemVisibilities",
                column: "VaultMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaultCryptoWallets");

            migrationBuilder.DropTable(
                name: "VaultDocuments");

            migrationBuilder.DropTable(
                name: "VaultItemVisibilities");

            migrationBuilder.DropTable(
                name: "VaultLinks");

            migrationBuilder.DropTable(
                name: "VaultNotes");

            migrationBuilder.DropTable(
                name: "VaultPasswords");

            migrationBuilder.DropTable(
                name: "VaultItems");
        }
    }
}
