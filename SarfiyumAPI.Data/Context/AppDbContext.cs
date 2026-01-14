using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.ValueObjects;
using System.Reflection.Emit;

namespace SarfiyumAPI.Data.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>, IDataContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        // --- 🔥 YENİ EKLENEN TABLOLAR ---
        public DbSet<TenantProductCategory> TenantProductCategories { get; set; }
        public DbSet<TenantProduct> TenantProducts { get; set; }

        public DbSet<TenantPaymentHistory> TenantPaymentHistories { get; set; }

        public DbSet<UserLoginHistory> UserLoginHistories { get; set; }

        public DbSet<UserActiveSession> UserActiveSessions { get; set; }

        public DbSet<SystemCatalog> SystemCatalogs { get; set; }



        // --- 🗑️ SİLİNEN ESKİ TABLOLAR ---
        // public DbSet<SymbolCategory> SymbolCategories { get; set; } 
        // public DbSet<Symbol> Symbols { get; set; }
        // public DbSet<TenantMultiplier> TenantMultipliers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. KULLANICI & TENANT İLİŞKİLERİ
            builder.Entity<AppUser>()
                .HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tenant>().OwnsOne(t => t.Address, a =>
            {
                a.Property(p => p.City).HasColumnName("City").HasMaxLength(50);
                a.Property(p => p.District).HasColumnName("District").HasMaxLength(50);
                a.Property(p => p.FullAddress).HasColumnName("FullAddress").HasMaxLength(500);
                a.Property(p => p.ZipCode).HasColumnName("ZipCode").HasMaxLength(20);
                a.Property(p => p.Country).HasColumnName("Country").HasMaxLength(50);
            });

            // Tenant ile Ödeme Geçmişi Arasındaki İlişki
            builder.Entity<Tenant>()
                .HasMany(t => t.PaymentHistories) // Tenant entity'sine eklediğin property
                .WithOne()
                .HasForeignKey(h => h.TenantId)
                .OnDelete(DeleteBehavior.Cascade); // Firma silinirse geçmişi de silinsin

            // Para birimi hassasiyeti (Önemli)
            builder.Entity<TenantPaymentHistory>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)"); // Para birimi için standart (virgülden sonra 2 hane)

            // =========================================================================
            //  🔥 TENANT PRODUCT & CATEGORY AYARLARI (YENİ)
            // =========================================================================

            // --- TenantProductCategory ---
            builder.Entity<TenantProductCategory>()
                .HasMany(c => c.TenantProducts)
                .WithOne(p => p.TenantProductCategory)
                .HasForeignKey(p => p.TenantProductCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Kategori silinirse ürünler ne olsun? (Restrict: Silinemez, Cascade: Silinir)

            // --- TenantProduct ---
            // Decimal Hassasiyetleri (Çarpanlar için)
            builder.Entity<TenantProduct>()
                .Property(p => p.BuyMultiplier).HasColumnType("decimal(18,10)");

            builder.Entity<TenantProduct>()
                .Property(p => p.SellMultiplier).HasColumnType("decimal(18,10)");

            builder.Entity<TenantProduct>()
                .Property(p => p.AddonAmount).HasColumnType("decimal(18,10)");

            // =========================================================================
            //  GLOBAL QUERY FILTERS (SOFT DELETE)
            // =========================================================================
            builder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);

            // Yeni Tablolar İçin Soft Delete Filtresi
            builder.Entity<TenantProductCategory>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<TenantProduct>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<TenantPaymentHistory>().HasQueryFilter(e => !e.IsDeleted);

            // =========================================================================
            //  SEED DATA (AYNEN KORUNDU)
            // =========================================================================

            const string ADMIN_ROLE_ID = "341743f0-asd2-42de-afbf-59kmkkmk72cf6";
            const string USER_ROLE_ID = "80f83660-321f-43f1-abc4-325256252256";
            const string ADMIN_USER_ID = "9245fe4a-d402-451c-b9ed-9c1a04247482";

            // Rolleri Ekle
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = ADMIN_ROLE_ID, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = USER_ROLE_ID, Name = "User", NormalizedName = "USER" }
            );

            // Admin Kullanıcısını Oluştur
            var adminUser = new AppUser
            {
                Id = ADMIN_USER_ID,
                UserName = "yvzsylu",
                NormalizedUserName = "YVZSYLU",
                Email = "yvzsylu@gmail.com",
                NormalizedEmail = "YVZSYLU@GMAIL.COM",
                EmailConfirmed = true,
                FullName = "Yavuz Soylu",
                TenantId = null,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Şifre Hashleme
            var passwordHasher = new PasswordHasher<AppUser>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Zs241994");

            builder.Entity<AppUser>().HasData(adminUser);

            // Kullanıcıya Admin Rolünü Ata
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    RoleId = ADMIN_ROLE_ID,
                    UserId = ADMIN_USER_ID
                }
            );



            builder.Entity<SystemCatalog>().HasData(

             // --- 1. ANA MADENLER (12 Adet) ---
             new SystemCatalog { SourceKey = "ALTIN", Name = "Has Altın" },
             new SystemCatalog { SourceKey = "GUMTRY", Name = "Gümüş (TL)" },
             new SystemCatalog { SourceKey = "GUMUSD", Name = "Gümüş (USD)" },
             new SystemCatalog { SourceKey = "KULCEALTIN", Name = "Külçe Altın" },
             new SystemCatalog { SourceKey = "XAUUSD", Name = "Ons Altın" },
             new SystemCatalog { SourceKey = "XAGUSD", Name = "Ons Gümüş" },
             new SystemCatalog { SourceKey = "XAUXAG", Name = "Altın / Gümüş Rasyosu" },
             new SystemCatalog { SourceKey = "XAUEUR", Name = "Ons Altın (Euro)" },
             new SystemCatalog { SourceKey = "XPTUSD", Name = "Ons Platin" },
             new SystemCatalog { SourceKey = "XPDUSD", Name = "Ons Paladyum" },
             new SystemCatalog { SourceKey = "PLATIN", Name = "Platin (TL)" },
             new SystemCatalog { SourceKey = "PALADYUM", Name = "Paladyum (TL)" },

             // --- 2. GRAM ALTINLAR (7 Adet) ---
             new SystemCatalog { SourceKey = "AYAR22", Name = "22 Ayar Gram" },
             new SystemCatalog { SourceKey = "AYAR14", Name = "14 Ayar Gram" },
             new SystemCatalog { SourceKey = "5 Gr Gram Altın", Name = "5 Gram Altın" },
             new SystemCatalog { SourceKey = "10 Gr Gram Altın", Name = "10 Gram Altın" },
             new SystemCatalog { SourceKey = "20 Gr Gram Altın", Name = "20 Gram Altın" },
             new SystemCatalog { SourceKey = "50 Gr Gram Altın", Name = "50 Gram Altın" },
             new SystemCatalog { SourceKey = "100 Gr Gram Altın", Name = "100 Gram Altın" },

             // --- 3. DÖVİZLER (TRY KARŞILIKLARI) (11 Adet) ---
             new SystemCatalog { SourceKey = "USDTRY", Name = "Dolar / TL" },
             new SystemCatalog { SourceKey = "EURTRY", Name = "Euro / TL" },
             new SystemCatalog { SourceKey = "GBPTRY", Name = "Sterlin / TL" },
             new SystemCatalog { SourceKey = "CHFTRY", Name = "İsviçre Frangı" },
             new SystemCatalog { SourceKey = "AUDTRY", Name = "Avustralya Doları" },
             new SystemCatalog { SourceKey = "CADTRY", Name = "Kanada Doları" },
             new SystemCatalog { SourceKey = "SARTRY", Name = "Suudi Arabistan Riyali" },
             new SystemCatalog { SourceKey = "JPYTRY", Name = "Japon Yeni" },
             new SystemCatalog { SourceKey = "NOKTRY", Name = "Norveç Kronu" },
             new SystemCatalog { SourceKey = "SEKTRY", Name = "İsveç Kronu" },
             new SystemCatalog { SourceKey = "DKKTRY", Name = "Danimarka Kronu" },

             // --- 4. PARİTELER (ÇAPRAZ KURLAR) (13 Adet) ---
             new SystemCatalog { SourceKey = "EURUSD", Name = "Euro / Dolar" },
             new SystemCatalog { SourceKey = "GBPUSD", Name = "Sterlin / Dolar" },
             new SystemCatalog { SourceKey = "USDCHF", Name = "Dolar / İsviçre Frangı" },
             new SystemCatalog { SourceKey = "USDCAD", Name = "Dolar / Kanada Doları" },
             new SystemCatalog { SourceKey = "AUDUSD", Name = "Avustralya Doları / Dolar" },
             new SystemCatalog { SourceKey = "USDSAR", Name = "Dolar / Suudi Riyali" },
             new SystemCatalog { SourceKey = "USDJPY", Name = "Dolar / Japon Yeni" },
             new SystemCatalog { SourceKey = "USDNOK", Name = "Dolar / Norveç Kronu" },
             new SystemCatalog { SourceKey = "USDSEK", Name = "Dolar / İsveç Kronu" },
             new SystemCatalog { SourceKey = "USDDKK", Name = "Dolar / Danimarka Kronu" },
             new SystemCatalog { SourceKey = "USDRUB", Name = "Dolar / Ruble" },
             new SystemCatalog { SourceKey = "EURGBP", Name = "Euro / Sterlin" },
             new SystemCatalog { SourceKey = "EURCHF", Name = "Euro / İsviçre Frangı" },

             // --- 5. ZİYNET GRUBU (YENİ TARİHLİ) (6 Adet) ---
             new SystemCatalog { SourceKey = "CEYREK_YENI", Name = "Yeni Çeyrek" },
             new SystemCatalog { SourceKey = "YARIM_YENI", Name = "Yeni Yarım" },
             new SystemCatalog { SourceKey = "TEK_YENI", Name = "Yeni Tam" },
             new SystemCatalog { SourceKey = "ATA_YENI", Name = "Yeni Ata" },
             new SystemCatalog { SourceKey = "ATA5_YENI", Name = "Yeni 5'li Ata" },
             new SystemCatalog { SourceKey = "GREMESE_YENI", Name = "Yeni Gremse" },

             // --- 6. ZİYNET GRUBU (ESKİ TARİHLİ) (6 Adet) ---
             new SystemCatalog { SourceKey = "CEYREK_ESKI", Name = "Eski Çeyrek" },
             new SystemCatalog { SourceKey = "YARIM_ESKI", Name = "Eski Yarım" },
             new SystemCatalog { SourceKey = "TEK_ESKI", Name = "Eski Tam" },
             new SystemCatalog { SourceKey = "ATA_ESKI", Name = "Eski Ata" },
             new SystemCatalog { SourceKey = "ATA5_ESKI", Name = "Eski 5'li Ata" },
             new SystemCatalog { SourceKey = "GREMESE_ESKI", Name = "Eski Gremse" },

             // --- 7. BANKA / PARİTE KODLARI (PAR Prefix) (4 Adet) ---
             new SystemCatalog { SourceKey = "PARUSD", Name = "Parite USD" },
             new SystemCatalog { SourceKey = "PAREUR", Name = "Parite Euro" },
             new SystemCatalog { SourceKey = "PARGBP", Name = "Parite Sterlin" },
             new SystemCatalog { SourceKey = "PARCHF", Name = "Parite İsviçre Frangı" },

             // --- 8. DİĞER / ÖZEL KODLAR (7 Adet) ---
             new SystemCatalog { SourceKey = "XAUUSDS", Name = "Ons Altın (Sanal)" },
             new SystemCatalog { SourceKey = "EURUSDS", Name = "Euro / Dolar (Sanal)" },
             new SystemCatalog { SourceKey = "KXAGUSD", Name = "Kilo Gümüş (USD)" },
             new SystemCatalog { SourceKey = "XUSDTRY", Name = "Dolar / TL (Spot)" },
             new SystemCatalog { SourceKey = "FARKEUR", Name = "Fark Euro" },
             new SystemCatalog { SourceKey = "FARK", Name = "Fark Altın" },
             new SystemCatalog { SourceKey = "VADE FARK", Name = "Vade Farkı" }
             );

        }
    }
}