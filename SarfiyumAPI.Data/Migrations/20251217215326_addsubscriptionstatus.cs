using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class addsubscriptionstatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TenantPaymentHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3c3349fc-d45a-4f1b-a36c-2fc952ecde39", "AQAAAAIAAYagAAAAEKpIwIhh8u47/pt2USoH7dz6iB7/878OWOml8qxY1l0qlkc9GoirVMSpOp9n+hq7Nw==", "0a57d734-2c91-4fb4-bcb1-106cefcbb7b8" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "TenantPaymentHistories");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "dc4568af-d3ed-4b40-ba5d-90c9d2b4847b", "AQAAAAIAAYagAAAAEFvDiv7NTyVA93llcF0ftPiiuGQ6+WxdjQk0QaHk6vD9g6t8u2u0/B4UHCsityfd9w==", "355eb26a-2741-4b5d-ba63-7b5a18998f8f" });
        }
    }
}
