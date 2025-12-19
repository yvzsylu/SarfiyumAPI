using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class tenanthistoryentityfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "dc4568af-d3ed-4b40-ba5d-90c9d2b4847b", "AQAAAAIAAYagAAAAEFvDiv7NTyVA93llcF0ftPiiuGQ6+WxdjQk0QaHk6vD9g6t8u2u0/B4UHCsityfd9w==", "355eb26a-2741-4b5d-ba63-7b5a18998f8f" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bb349fa0-1c15-46b1-8430-5a86b39c9aa8", "AQAAAAIAAYagAAAAEHEY1AA4/P/cPn95f/Bvt1zD++G0gffZMdvktkexv8FCEW8fC84E23GVL3ah/j6Z1A==", "4650bd0b-e516-4c25-8762-723b4e789c9a" });
        }
    }
}
