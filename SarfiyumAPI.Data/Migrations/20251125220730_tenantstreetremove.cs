using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class tenantstreetremove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Street",
                table: "Tenants");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9c9a2820-af55-4b44-8f52-25b74bf10ee1", "AQAAAAIAAYagAAAAEJ5VaMLww5XFF6oe+mLZ1ORrCWs6/wScvusG9Br5csNo1MUwrRVHLF2sMchfd4U8wA==", "42561c35-d701-4e87-b742-f3ce19850d63" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Tenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "74e6f940-fc42-4aa9-aca0-2be988907aab", "AQAAAAIAAYagAAAAEM3/ZB0kvtx1rfqD9M6gIaTBF5anhjE8FRKIHpS5igAAZda/lidewnDjwpDI71ytnA==", "7d67d605-d38e-4bd9-a6ec-36b1efa8a44a" });
        }
    }
}
