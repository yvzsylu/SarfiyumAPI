using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class addsymbolsandtenantmultiplierentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ModifiedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    RemovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RemovedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantMultipliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SymbolId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    BuyMultiplier = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SellMultiplier = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    AddonAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ModifiedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    RemovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RemovedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMultipliers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMultipliers_Symbols_SymbolId",
                        column: x => x.SymbolId,
                        principalTable: "Symbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2087021f-72d0-4e15-9bc9-10ada8d69a84", "AQAAAAIAAYagAAAAEI7+tm3yS+V7aAnO2ZJ0kGTeEcLBfhc4nhgqf8Ncr+/2ZRYEUq/RuXKpyRK/Wv4M2Q==", "429e16f2-9fd6-45b5-8d6e-e64966ba710d" });

            migrationBuilder.CreateIndex(
                name: "IX_Symbols_ApiKey",
                table: "Symbols",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMultipliers_SymbolId",
                table: "TenantMultipliers",
                column: "SymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMultipliers_TenantId_SymbolId",
                table: "TenantMultipliers",
                columns: new[] { "TenantId", "SymbolId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMultipliers");

            migrationBuilder.DropTable(
                name: "Symbols");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "caaaf99b-5e4d-49ff-a045-ac45c04af10b", "AQAAAAIAAYagAAAAEMdmr81vSf9DG59McLdUz7o3qDRcA8wgaYX1J7ppAwAd/lv0tLdwLuVGcv1latVg1Q==", "d007bfab-472f-4c3c-b439-9366ab034f23" });
        }
    }
}
