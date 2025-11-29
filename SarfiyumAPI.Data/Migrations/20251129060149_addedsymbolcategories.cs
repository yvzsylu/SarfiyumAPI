using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class addedsymbolcategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Symbols");

            migrationBuilder.RenameColumn(
                name: "ApiKey",
                table: "Symbols",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Symbols_ApiKey",
                table: "Symbols",
                newName: "IX_Symbols_Name");

            migrationBuilder.AddColumn<string>(
                name: "SymbolCategoryId",
                table: "Symbols",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SymbolCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_SymbolCategories", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "dd4617cc-3359-4737-a761-4be687ff5980", "AQAAAAIAAYagAAAAEH4RmQe4z64J4+Lqd1j4YWvjhmSH+UtwsUGPU28fgaGwXy5p/5tTe6716OYby61JGQ==", "d9ff5093-332e-4fa5-87d6-945875e1cca2" });

            migrationBuilder.CreateIndex(
                name: "IX_Symbols_SymbolCategoryId",
                table: "Symbols",
                column: "SymbolCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SymbolCategories_Name",
                table: "SymbolCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Symbols_SymbolCategories_SymbolCategoryId",
                table: "Symbols",
                column: "SymbolCategoryId",
                principalTable: "SymbolCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Symbols_SymbolCategories_SymbolCategoryId",
                table: "Symbols");

            migrationBuilder.DropTable(
                name: "SymbolCategories");

            migrationBuilder.DropIndex(
                name: "IX_Symbols_SymbolCategoryId",
                table: "Symbols");

            migrationBuilder.DropColumn(
                name: "SymbolCategoryId",
                table: "Symbols");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Symbols",
                newName: "ApiKey");

            migrationBuilder.RenameIndex(
                name: "IX_Symbols_Name",
                table: "Symbols",
                newName: "IX_Symbols_ApiKey");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Symbols",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Symbols",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2087021f-72d0-4e15-9bc9-10ada8d69a84", "AQAAAAIAAYagAAAAEI7+tm3yS+V7aAnO2ZJ0kGTeEcLBfhc4nhgqf8Ncr+/2ZRYEUq/RuXKpyRK/Wv4M2Q==", "429e16f2-9fd6-45b5-8d6e-e64966ba710d" });
        }
    }
}
