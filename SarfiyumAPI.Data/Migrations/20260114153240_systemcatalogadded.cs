using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SarfiyumAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class systemcatalogadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemCatalogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_SystemCatalogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c43ca297-5fda-469d-8f29-e9b5644c0296", "AQAAAAIAAYagAAAAEPjXgbs4cArNJvpysX0dWvw08NuaJav5gMxj7QTI09YjjLpClAza+8PnGhJ/2Rhn3g==", "277d1e79-7247-4e82-9bfb-314fdb3f8467" });

            migrationBuilder.InsertData(
                table: "SystemCatalogs",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "CreatedByName", "IsDeleted", "ModifiedAt", "ModifiedById", "ModifiedByName", "Name", "RemovedAt", "RemovedById", "RemovedByName", "SourceKey" },
                values: new object[,]
                {
                    { "0147833d-749b-44c8-8713-927e42d56a10", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4898), null, "", false, null, null, null, "Külçe Altın", null, null, null, "KULCEALTIN" },
                    { "021e2605-25af-4fae-890f-8a5db05eb939", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4914), null, "", false, null, null, null, "Platin (TL)", null, null, null, "PLATIN" },
                    { "04851f05-3bc4-4a7e-8189-0ef7d878d9f6", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4998), null, "", false, null, null, null, "Eski Tam", null, null, null, "TEK_ESKI" },
                    { "137336e1-c8d2-4c76-93e0-42ad54dd6ccd", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5006), null, "", false, null, null, null, "Parite Euro", null, null, null, "PAREUR" },
                    { "1710bda0-788b-492d-95c7-7fdf8f56fb3f", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4923), null, "", false, null, null, null, "20 Gram Altın", null, null, null, "20 Gr Gram Altın" },
                    { "184ec665-0169-42a0-8e99-146ca3a2f00d", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5004), null, "", false, null, null, null, "Parite USD", null, null, null, "PARUSD" },
                    { "24c28516-7a23-4161-bbc9-22de27d88d62", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4980), null, "", false, null, null, null, "Dolar / İsveç Kronu", null, null, null, "USDSEK" },
                    { "2cb2215f-bfd2-41b2-8c0c-f7460e10f308", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4941), null, "", false, null, null, null, "Danimarka Kronu", null, null, null, "DKKTRY" },
                    { "3201efa4-ca19-4f05-afb0-cc13127fde6e", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5012), null, "", false, null, null, null, "Kilo Gümüş (USD)", null, null, null, "KXAGUSD" },
                    { "3420a40e-e199-414e-bca6-72e00e3d6f4a", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4897), null, "", false, null, null, null, "Gümüş (USD)", null, null, null, "GUMUSD" },
                    { "34793bba-e600-4977-97e4-2ecb6a820df1", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4915), null, "", false, null, null, null, "Paladyum (TL)", null, null, null, "PALADYUM" },
                    { "354ac8ba-9605-4b41-a2d8-cd0d52e3cf08", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5016), null, "", false, null, null, null, "Fark Euro", null, null, null, "FARKEUR" },
                    { "37cc9326-61b9-44cc-94ae-96d1d1651ce6", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4909), null, "", false, null, null, null, "Ons Altın (Euro)", null, null, null, "XAUEUR" },
                    { "38dfec6e-3e32-4bc5-b9ec-3ee6ffc2c026", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4916), null, "", false, null, null, null, "22 Ayar Gram", null, null, null, "AYAR22" },
                    { "3db65fef-2106-403a-a916-31596dde2e8d", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5009), null, "", false, null, null, null, "Ons Altın (Sanal)", null, null, null, "XAUUSDS" },
                    { "3e5e0a19-40e4-4282-bc2b-ea6cab426c28", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4812), null, "", false, null, null, null, "Has Altın", null, null, null, "ALTIN" },
                    { "4906a5ab-043f-4f74-80ae-e2af76061443", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4987), null, "", false, null, null, null, "Yeni Çeyrek", null, null, null, "CEYREK_YENI" },
                    { "5001e96f-cbb5-4c24-92b1-81ab5df2b899", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4989), null, "", false, null, null, null, "Yeni Tam", null, null, null, "TEK_YENI" },
                    { "53225a17-74c6-48e2-a46c-e6fe0bb6c8cc", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5018), null, "", false, null, null, null, "Vade Farkı", null, null, null, "VADE FARK" },
                    { "57bc2753-9f09-4b6b-be97-6450ccef22a2", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4977), null, "", false, null, null, null, "Dolar / Norveç Kronu", null, null, null, "USDNOK" },
                    { "59a5f15e-f19e-49fd-ae47-a4e207653255", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4994), null, "", false, null, null, null, "Yeni Gremse", null, null, null, "GREMESE_YENI" },
                    { "5af4a3fc-a83a-4199-bdd1-9121d6ddb643", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4932), null, "", false, null, null, null, "İsviçre Frangı", null, null, null, "CHFTRY" },
                    { "6523542b-6d14-4d59-9b8d-a0890b41cba1", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4900), null, "", false, null, null, null, "Ons Altın", null, null, null, "XAUUSD" },
                    { "698b1a23-debe-49f6-a9ac-7706df0c6ff4", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4935), null, "", false, null, null, null, "Kanada Doları", null, null, null, "CADTRY" },
                    { "6f34fe47-4cad-4784-9a75-2d4f0158481e", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4908), null, "", false, null, null, null, "Altın / Gümüş Rasyosu", null, null, null, "XAUXAG" },
                    { "77776985-f939-430b-b2ab-4f3518cd3511", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4918), null, "", false, null, null, null, "14 Ayar Gram", null, null, null, "AYAR14" },
                    { "79745894-e3fd-4372-863a-567dff9c25e6", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4945), null, "", false, null, null, null, "Sterlin / Dolar", null, null, null, "GBPUSD" },
                    { "8030b298-52a8-41fc-93d8-1e58745fa3b1", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4925), null, "", false, null, null, null, "50 Gram Altın", null, null, null, "50 Gr Gram Altın" },
                    { "823ede87-005d-4516-bb9a-5fe2e5d2351e", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4984), null, "", false, null, null, null, "Euro / Sterlin", null, null, null, "EURGBP" },
                    { "885e4a1f-69e0-448b-994e-699e1dad4774", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5007), null, "", false, null, null, null, "Parite Sterlin", null, null, null, "PARGBP" },
                    { "8d92f0e5-1cd8-45da-9147-707616d6503a", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4988), null, "", false, null, null, null, "Yeni Yarım", null, null, null, "YARIM_YENI" },
                    { "90746d2f-3fa1-4ac7-b7d4-3709a77e0c7c", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4937), null, "", false, null, null, null, "Japon Yeni", null, null, null, "JPYTRY" },
                    { "90c73657-0ad1-442e-b3a7-def873707eed", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4950), null, "", false, null, null, null, "Avustralya Doları / Dolar", null, null, null, "AUDUSD" },
                    { "917dfe87-2f5d-4ec9-8c3a-e1adf09e224e", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4947), null, "", false, null, null, null, "Dolar / İsviçre Frangı", null, null, null, "USDCHF" },
                    { "94cd503f-8457-442a-a4c0-002f1cb13ccd", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5014), null, "", false, null, null, null, "Dolar / TL (Spot)", null, null, null, "XUSDTRY" },
                    { "97dd4590-714e-4133-bd14-93f85291576e", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4985), null, "", false, null, null, null, "Euro / İsviçre Frangı", null, null, null, "EURCHF" },
                    { "9c0e53e6-805c-4105-a25a-3660db9be7ad", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4997), null, "", false, null, null, null, "Eski Yarım", null, null, null, "YARIM_ESKI" },
                    { "9d990c56-83aa-4c8b-90fb-44e3829c2e49", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4993), null, "", false, null, null, null, "Yeni 5'li Ata", null, null, null, "ATA5_YENI" },
                    { "a1d3de16-2701-4b5c-b4a0-00b4a5409e7a", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4922), null, "", false, null, null, null, "10 Gram Altın", null, null, null, "10 Gr Gram Altın" },
                    { "a2cd6f5b-6522-4fe2-bbf7-dc7decd05693", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5011), null, "", false, null, null, null, "Euro / Dolar (Sanal)", null, null, null, "EURUSDS" },
                    { "a312277b-56fb-42ee-bb5c-f67ff38ce929", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4999), null, "", false, null, null, null, "Eski Ata", null, null, null, "ATA_ESKI" },
                    { "a60dfeed-7460-4067-b908-ba7ae44e6aae", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4983), null, "", false, null, null, null, "Dolar / Ruble", null, null, null, "USDRUB" },
                    { "a972cd4c-c210-431e-81cc-30660549d517", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4933), null, "", false, null, null, null, "Avustralya Doları", null, null, null, "AUDTRY" },
                    { "b0229da4-4c8d-4464-adfc-a62b099dc15d", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4951), null, "", false, null, null, null, "Dolar / Suudi Riyali", null, null, null, "USDSAR" },
                    { "b2c16352-8e68-4a37-b575-70bf29a3c54c", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4995), null, "", false, null, null, null, "Eski Çeyrek", null, null, null, "CEYREK_ESKI" },
                    { "b6e0532e-6f87-42a0-8e24-798b76bd3da4", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4953), null, "", false, null, null, null, "Dolar / Japon Yeni", null, null, null, "USDJPY" },
                    { "b6f84cd3-1d30-4d44-b99d-ceb1a6105d7b", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4936), null, "", false, null, null, null, "Suudi Arabistan Riyali", null, null, null, "SARTRY" },
                    { "b92d3385-fa8b-42db-805f-f1578e034301", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4927), null, "", false, null, null, null, "Dolar / TL", null, null, null, "USDTRY" },
                    { "be776cc6-62dc-495b-a48d-01abe09621d7", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5001), null, "", false, null, null, null, "Eski 5'li Ata", null, null, null, "ATA5_ESKI" },
                    { "c320d992-2c81-425b-a78f-2998cfd4e44c", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5003), null, "", false, null, null, null, "Eski Gremse", null, null, null, "GREMESE_ESKI" },
                    { "c392f349-31bb-4ae6-ae15-83340ccf0216", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5008), null, "", false, null, null, null, "Parite İsviçre Frangı", null, null, null, "PARCHF" },
                    { "c5d40999-6df0-4175-ae60-20b348a9efb8", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4911), null, "", false, null, null, null, "Ons Platin", null, null, null, "XPTUSD" },
                    { "c71bff00-9390-49bb-b556-27b43e4800f9", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4940), null, "", false, null, null, null, "İsveç Kronu", null, null, null, "SEKTRY" },
                    { "ca3c9293-2aae-4dcd-99b9-961e8db9fa40", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4926), null, "", false, null, null, null, "100 Gram Altın", null, null, null, "100 Gr Gram Altın" },
                    { "cebcbea4-ce87-4b18-a092-7fe8eed8e7d3", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4921), null, "", false, null, null, null, "5 Gram Altın", null, null, null, "5 Gr Gram Altın" },
                    { "cee74807-5ee5-40e7-8229-9f2c9c479cdd", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4944), null, "", false, null, null, null, "Euro / Dolar", null, null, null, "EURUSD" },
                    { "cf1a6a0b-91e7-4e10-8f3a-b1bb3f6efa91", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4817), null, "", false, null, null, null, "Gümüş (TL)", null, null, null, "GUMTRY" },
                    { "d5bd049b-e61a-4aed-9ff6-8cf3dbf24933", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4929), null, "", false, null, null, null, "Euro / TL", null, null, null, "EURTRY" },
                    { "da262fdf-f2fb-4d7c-8ae5-08195ad087bf", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4912), null, "", false, null, null, null, "Ons Paladyum", null, null, null, "XPDUSD" },
                    { "dc9fee37-6956-4710-9975-745f2f3edebb", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4938), null, "", false, null, null, null, "Norveç Kronu", null, null, null, "NOKTRY" },
                    { "e0268a60-5b47-4e0d-8a3f-f07103e6d99a", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4949), null, "", false, null, null, null, "Dolar / Kanada Doları", null, null, null, "USDCAD" },
                    { "e51cb105-31f4-4330-b2c7-337591459c8c", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4930), null, "", false, null, null, null, "Sterlin / TL", null, null, null, "GBPTRY" },
                    { "e58e9cf4-3520-4e2b-b429-7bd6e8f8272b", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(5017), null, "", false, null, null, null, "Fark Altın", null, null, null, "FARK" },
                    { "e894de6c-91de-4f22-8bc9-b2a7d45e97ad", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4981), null, "", false, null, null, null, "Dolar / Danimarka Kronu", null, null, null, "USDDKK" },
                    { "e8ff9ece-5b95-4ebc-94d5-1688645d4e8f", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4991), null, "", false, null, null, null, "Yeni Ata", null, null, null, "ATA_YENI" },
                    { "f30adef0-4c02-4e7b-b35e-f5eff097dea4", new DateTime(2026, 1, 14, 15, 32, 40, 63, DateTimeKind.Utc).AddTicks(4901), null, "", false, null, null, null, "Ons Gümüş", null, null, null, "XAGUSD" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemCatalogs");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9245fe4a-d402-451c-b9ed-9c1a04247482",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "24752e1f-935b-4c2e-9ff9-bb6e9b8890e2", "AQAAAAIAAYagAAAAEDJPsydUAug5WYp1/Itotbg8pWygJA+AiTtUfWGDqHVDVfURdQJxhc1alY0mtkpvFA==", "082884e9-1939-47f0-be69-fb4c0c9dd215" });
        }
    }
}
