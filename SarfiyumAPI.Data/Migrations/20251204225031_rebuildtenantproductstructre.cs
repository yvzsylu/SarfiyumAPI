using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class rebuildtenantproductstructre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMultipliers");

            migrationBuilder.DropTable(
                name: "Symbols");

            migrationBuilder.DropTable(
                name: "SymbolCategories");

            migrationBuilder.CreateTable(
                name: "TenantProductCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TenantProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantProducts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantProductCategoryId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuyMultiplier = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    SellMultiplier = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    AddonAmount = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TenantProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantProducts_TenantProductCategories_TenantProductCategoryId",
                        column: x => x.TenantProductCategoryId,
                        principalTable: "TenantProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9b6800f3-2c45-43ac-9e09-941f8f7c2300", "AQAAAAIAAYagAAAAEDsnulnzFq3qarxrP1DXQrEQEJlJ54KcTlwAqoR/OQq9sGSeJB387UBuRap+LYDSNg==", "c56600c0-34fd-45ff-b2dd-b1f555941a96" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantProducts_TenantProductCategoryId",
                table: "TenantProducts",
                column: "TenantProductCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantProducts");

            migrationBuilder.DropTable(
                name: "TenantProductCategories");

            migrationBuilder.CreateTable(
                name: "SymbolCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    RemovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymbolCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SymbolCategoryId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    RemovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Symbols_SymbolCategories_SymbolCategoryId",
                        column: x => x.SymbolCategoryId,
                        principalTable: "SymbolCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantMultipliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SymbolId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    AddonAmount = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    BuyMultiplier = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ModifiedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedById = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    RemovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SellMultiplier = table.Column<decimal>(type: "decimal(18,10)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false)
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
                values: new object[] { "f5b50993-7f0d-4bcf-8648-c9e795b62a95", "AQAAAAIAAYagAAAAEFfXYnW27NubeLcCUf9XFFi6cbTNoYQdRE+mi3iR/3y6enN1KTJCeTvo24kPJ6YZGQ==", "f275966c-462a-45f9-afa6-55317ced98ef" });

            migrationBuilder.CreateIndex(
                name: "IX_SymbolCategories_Name",
                table: "SymbolCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Symbols_Name",
                table: "Symbols",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Symbols_SymbolCategoryId",
                table: "Symbols",
                column: "SymbolCategoryId");

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
    }
}
