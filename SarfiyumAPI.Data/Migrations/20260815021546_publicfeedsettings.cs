using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SarfiyumAPI.Data.Migrations
{
    /// <summary>
    /// Girişsiz (public) fiyat ekranı için <c>Tenants</c> tablosuna iki kolon ekler.
    ///
    /// ⚠️ <b>Bu dosya EL İLE SADELEŞTİRİLMİŞTİR.</b> EF'in ürettiği hâli 56 KB'tı ve
    /// içinde 66 <c>SystemCatalogs</c> satırının silinip yeniden eklenmesi vardı.
    /// Sebep: seed kayıtları sabit <c>Id</c> taşımıyor, <c>EntityBase.Id</c> varsayılanı
    /// <c>Guid.NewGuid()</c> olduğu için her model derlemesinde yeni GUID üretiliyor ve
    /// EF bunu "satırlar değişti" sanıyor. Katalog verisinin İÇERİĞİ (SourceKey/Name)
    /// hiç değişmiyor; yalnızca id'ler rastgele yeniden üretiliyordu.
    ///
    /// Bu churn her migration'da tekrarlıyor (önceki migration'larda da var). Canlı
    /// veritabanında 66 satırı gereksiz yere silip yeniden yazmamak için buradan
    /// çıkarıldı. Kalıcı çözümü: seed kayıtlarına sabit <c>Id</c> vermek.
    /// </summary>
    public partial class publicfeedsettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublicFeedEnabled",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicSlug",
                table: "Tenants",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublicFeedEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PublicSlug",
                table: "Tenants");
        }
    }
}
