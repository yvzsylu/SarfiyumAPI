using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class roleandadminuserseeddata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "341743f0-asd2-42de-afbf-59kmkkmk72cf6", null, "Admin", "ADMIN" },
                    { "80f83660-321f-43f1-abc4-325256252256", null, "User", "USER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "FullName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TenantId", "TwoFactorEnabled", "UserName" },
                values: new object[] { "9245fe4a-d402-451c-b9ed-9c1a04247482", 0, "551aa076-45a2-478c-8522-3f6a6db24676", "yvzsylu@gmail.com", true, "Yavuz Soylu", false, null, "YVZSYLU@GMAIL.COM", "YVZSYLU", "AQAAAAIAAYagAAAAEDocxgg78BvSDWzKL2BTdqJnScNvOu+jQMBAmc+ig7AgEf6wbEv2J+teU+W50Asi4w==", null, false, "76720c6c-7334-416c-ace5-94879b5fd2e8", null, false, "yvzsylu" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "341743f0-asd2-42de-afbf-59kmkkmk72cf6", "9245fe4a-d402-451c-b9ed-9c1a04247482" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "80f83660-321f-43f1-abc4-325256252256");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "341743f0-asd2-42de-afbf-59kmkkmk72cf6", "9245fe4a-d402-451c-b9ed-9c1a04247482" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "341743f0-asd2-42de-afbf-59kmkkmk72cf6");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482");
        }
    }
}
