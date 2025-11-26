using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "IsActive", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f9169de9-dffb-456d-bfd1-136a504094d1", true, "AQAAAAIAAYagAAAAEJIpyxr9Y9laAPrlamF69J5OL/ftHRJ+iRh+sj5hYfGGUicVVQbC3Eb7s5yFXLqwiA==", "e6016fc3-56f4-4bef-8be8-a61a9d4fe8c9" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "933081f2-0874-4627-a73a-4f64069bca9e", "AQAAAAIAAYagAAAAELcZPlel4NMfT3hQXikUrjkTMDFFcRvmYRRhVakK7dsrPJd1vI8NShC7UOU7e5XPPA==", "249505d4-56ad-4fe7-9c81-cb41f007a8c7" });
        }
    }
}
