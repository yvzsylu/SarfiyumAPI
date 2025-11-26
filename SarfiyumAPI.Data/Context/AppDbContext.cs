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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. İLİŞKİLER VE AYARLAR (Burada değişiklik yok)
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
            //  SEED DATA (BAŞLANGIÇ VERİLERİ) - Update-Database anında çalışır
            // =========================================================================

            // A. Sabit ID'ler (Migration her çalıştığında değişmesin diye sabitliyoruz)
            const string ADMIN_ROLE_ID = "341743f0-asd2-42de-afbf-59kmkkmk72cf6";
            const string USER_ROLE_ID = "80f83660-321f-43f1-abc4-325256252256";
            const string ADMIN_USER_ID = "9245fe4a-d402-451c-b9ed-9c1a04247482";

            // B. Rolleri Ekle
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = ADMIN_ROLE_ID, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = USER_ROLE_ID, Name = "User", NormalizedName = "USER" }
            );

            // C. Admin Kullanıcısını Oluştur
            var adminUser = new AppUser
            {
                Id = ADMIN_USER_ID,
                UserName = "yvzsylu",
                NormalizedUserName = "YVZSYLU",
                Email = "yvzsylu@gmail.com",
                NormalizedEmail = "YVZSYLU@GMAIL.COM",
                EmailConfirmed = true,
                FullName = "Yavuz Soylu",
                TenantId = null, // Süper Admin olduğu için Tenant YOK
                SecurityStamp = Guid.NewGuid().ToString()
            };

            // Şifre Hashleme (Zs241994)
            var passwordHasher = new PasswordHasher<AppUser>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Zs241994");

            builder.Entity<AppUser>().HasData(adminUser);

            // D. Kullanıcıya Admin Rolünü Ata
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