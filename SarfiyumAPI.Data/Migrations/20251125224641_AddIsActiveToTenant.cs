using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "933081f2-0874-4627-a73a-4f64069bca9e", "AQAAAAIAAYagAAAAELcZPlel4NMfT3hQXikUrjkTMDFFcRvmYRRhVakK7dsrPJd1vI8NShC7UOU7e5XPPA==", "249505d4-56ad-4fe7-9c81-cb41f007a8c7" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tenants");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9c9a2820-af55-4b44-8f52-25b74bf10ee1", "AQAAAAIAAYagAAAAEJ5VaMLww5XFF6oe+mLZ1ORrCWs6/wScvusG9Br5csNo1MUwrRVHLF2sMchfd4U8wA==", "42561c35-d701-4e87-b742-f3ce19850d63" });
        }
    }
}
