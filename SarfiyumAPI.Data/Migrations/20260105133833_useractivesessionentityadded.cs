using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class useractivesessionentityadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActiveSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientSource = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_UserActiveSessions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "24752e1f-935b-4c2e-9ff9-bb6e9b8890e2", "AQAAAAIAAYagAAAAEDJPsydUAug5WYp1/Itotbg8pWygJA+AiTtUfWGDqHVDVfURdQJxhc1alY0mtkpvFA==", "082884e9-1939-47f0-be69-fb4c0c9dd215" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActiveSessions");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a5991833-96b9-42b7-9f66-1ff28c7219c1", "AQAAAAIAAYagAAAAEK8NQ9+j9bCf4br0ZJMGrvKJCpVrzqbLSutxqhoq+m1LMLH8HlSydN0ntfNPl1C9NQ==", "9f1b46dc-50b7-4562-9095-7fc763f27766" });
        }
    }
}
