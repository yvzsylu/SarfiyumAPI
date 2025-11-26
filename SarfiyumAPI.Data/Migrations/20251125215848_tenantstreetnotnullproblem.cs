using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class tenantstreetnotnullproblem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Tenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "74e6f940-fc42-4aa9-aca0-2be988907aab", "AQAAAAIAAYagAAAAEM3/ZB0kvtx1rfqD9M6gIaTBF5anhjE8FRKIHpS5igAAZda/lidewnDjwpDI71ytnA==", "7d67d605-d38e-4bd9-a6ec-36b1efa8a44a" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Street",
                table: "Tenants",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "551aa076-45a2-478c-8522-3f6a6db24676", "AQAAAAIAAYagAAAAEDocxgg78BvSDWzKL2BTdqJnScNvOu+jQMBAmc+ig7AgEf6wbEv2J+teU+W50Asi4w==", "76720c6c-7334-416c-ace5-94879b5fd2e8" });
        }
    }
}
