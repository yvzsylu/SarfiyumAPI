using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.ValueObjects;

namespace SarfiyumAPI.Data.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>, IDataContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        // --- YENİ EKLENEN VE GÜNCELLENEN TABLOLAR ---
        public DbSet<SymbolCategory> SymbolCategories { get; set; } // YENİ
        public DbSet<Symbol> Symbols { get; set; }
        public DbSet<TenantMultiplier> TenantMultipliers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. İLİŞKİLER VE AYARLAR
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

            // =========================================================================
            //  SYMBOL & CATEGORY AYARLARI (GÜNCELLENDİ)
            // =========================================================================

            // --- SymbolCategory Ayarları ---
            builder.Entity<SymbolCategory>()
                .HasIndex(c => c.Name)
                .IsUnique(); // Aynı isimde kategori olmasın

            // --- Symbol Ayarları ---
            builder.Entity<Symbol>()
                .HasIndex(s => s.Name) // ApiKey kalktı, Name artık benzersiz alanımız
                .IsUnique();

            // Sembol - Kategori İlişkisi (Garanti altına alıyoruz)
            builder.Entity<Symbol>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Symbols)
                .HasForeignKey(s => s.SymbolCategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Kategori silinirse semboller boşa düşmesin, hata versin.

            // --- TenantMultiplier Ayarları ---
            builder.Entity<TenantMultiplier>()
                .HasIndex(t => new { t.TenantId, t.SymbolId }) // Bir tenant aynı sembolü 2 kere ekleyemesin
                .IsUnique();

            // Decimal hassasiyetleri
            builder.Entity<TenantMultiplier>()
                .Property(p => p.BuyMultiplier).HasColumnType("decimal(18,10)");

            builder.Entity<TenantMultiplier>()
                .Property(p => p.SellMultiplier).HasColumnType("decimal(18,10)");

            builder.Entity<TenantMultiplier>()
                .Property(p => p.AddonAmount).HasColumnType("decimal(18,10)");

            // =========================================================================
            //  GLOBAL QUERY FILTERS (SOFT DELETE)
            // =========================================================================
            builder.Entity<Symbol>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<TenantMultiplier>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<SymbolCategory>().HasQueryFilter(e => !e.IsDeleted); // Yeni eklendi

            // =========================================================================
            //  SEED DATA (BAŞLANGIÇ VERİLERİ)
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
        }
    }
}