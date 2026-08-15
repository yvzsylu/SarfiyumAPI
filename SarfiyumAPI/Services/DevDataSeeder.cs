using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Services
{
    /// <summary>
    /// LOKAL test ortamını tek seferde kullanılabilir hâle getirir.
    ///
    /// <para>
    /// Yalnızca <c>Development</c> ortamında çalışır ve <b>idempotenttir</b>: var olanı
    /// bozmaz, eksik olanı tamamlar. Amaç, testlere başlamadan önce admin panelinden elle
    /// firma/kullanıcı/viewer oluşturmakla vakit kaybedilmemesidir.
    /// </para>
    ///
    /// <para>
    /// Oluşturduğu düzen (şifreler <see cref="TestPassword"/>):
    /// <list type="bullet">
    /// <item><b>Firma:</b> "Test Kuyumculuk" — 1 yıllık aboneliği açık, public yayını
    /// <c>testkuyumcu</c> slug'ı ile AÇIK.</item>
    /// <item><b>kuyumcu</b> — <c>User</c> rolü. Yönetim ekranlarının tamamı.</item>
    /// <item><b>musteri1</b> — <c>Viewer</c>. Özel fiyatı YOK (standart grubu test etmek için).</item>
    /// <item><b>musteri2</b> — <c>Viewer</c>. Özel fiyat testleri bunun üzerinde yapılır.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// ⚠️ Firma oluşturma <see cref="ITenantService.CreateAsync"/> üzerinden yapılır; böylece
    /// kategoriler ve varsayılan ürünler canlıdakiyle <b>aynı kodla</b> seed edilir.
    /// </para>
    /// </summary>
    public class DevDataSeeder : IHostedService
    {
        public const string TestPassword = "Test1234!";
        public const string TenantName = "Test Kuyumculuk";
        public const string PublicSlug = "testkuyumcu";

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DevDataSeeder> _logger;

        public DevDataSeeder(IServiceProvider serviceProvider, ILogger<DevDataSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sp = scope.ServiceProvider;

                var tenantRepo = sp.GetRequiredService<IRepository<Tenant>>();
                var context = sp.GetRequiredService<IDataContext>();
                var userManager = sp.GetRequiredService<UserManager<AppUser>>();
                var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
                var tenantService = sp.GetRequiredService<ITenantService>();

                foreach (var role in new[] { "Admin", "User", "Viewer" })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        _logger.LogInformation($"[DEV_SEED] Rol oluşturuldu: {role}");
                    }
                }

                var tenant = await tenantRepo.Where(t => t.Name == TenantName).FirstOrDefaultAsync(cancellationToken);

                if (tenant == null)
                {
                    // Kategoriler ve varsayılan ürünler bu çağrının içinde seed ediliyor.
                    var result = await tenantService.CreateAsync(new TenantDto
                    {
                        Name = TenantName,
                        TaxNumber = "1234567890",
                        PhoneNumber = "05550000000",
                        IsActive = true,
                        City = "İstanbul",
                        District = "Kapalıçarşı",
                        Country = "Türkiye"
                    });

                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning($"[DEV_SEED] Firma oluşturulamadı: {string.Join(", ", result.Errors ?? new List<string>())}");
                        return;
                    }

                    tenant = await tenantRepo.Where(t => t.Name == TenantName).FirstOrDefaultAsync(cancellationToken);
                    if (tenant == null) return;

                    _logger.LogInformation("[DEV_SEED] Test firması oluşturuldu (kategoriler + varsayılan ürünlerle).");
                }

                // Abonelik ve public yayın ayarları her açılışta garanti altına alınır:
                // abonelik süresi geçmişse yayın durur ve testler sessizce boş kalırdı.
                var needsSave = false;

                if (!tenant.SubscriptionEndDate.HasValue || tenant.SubscriptionEndDate.Value < DateTime.UtcNow.AddDays(7))
                {
                    tenant.SubscriptionEndDate = DateTime.UtcNow.AddYears(1);
                    needsSave = true;
                }

                if (!tenant.IsPublicFeedEnabled || tenant.PublicSlug != PublicSlug)
                {
                    tenant.IsPublicFeedEnabled = true;
                    tenant.PublicSlug = PublicSlug;
                    needsSave = true;
                }

                if (needsSave)
                {
                    tenantRepo.Update(tenant);
                    await context.SaveChangesAsync();
                    tenantService.ClearTenantCache();
                    _logger.LogInformation("[DEV_SEED] Abonelik ve public yayın ayarları güncellendi.");
                }

                await EnsureUserAsync(userManager, "kuyumcu", "Test Kuyumcu", "User", tenant.Id);
                await EnsureUserAsync(userManager, "musteri1", "Müşteri Bir", "Viewer", tenant.Id);
                await EnsureUserAsync(userManager, "musteri2", "Müşteri İki", "Viewer", tenant.Id);

                _logger.LogInformation(
                    "[DEV_SEED] Hazır. Giriş: kuyumcu / musteri1 / musteri2 — şifre: {Password}. Public slug: {Slug}",
                    TestPassword, PublicSlug);
            }
            catch (Exception ex)
            {
                // Seed hatası uygulamayı başlatmamazlık etmemeli; testçi logdan görür.
                _logger.LogError($"[DEV_SEED] Seed sırasında hata: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task EnsureUserAsync(
            UserManager<AppUser> userManager,
            string userName,
            string fullName,
            string role,
            string tenantId)
        {
            var user = await userManager.FindByNameAsync(userName);

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = userName,
                    Email = $"{userName}@test.local",
                    FullName = fullName,
                    IsActive = true,
                    TenantId = tenantId,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, TestPassword);

                if (!created.Succeeded)
                {
                    _logger.LogWarning(
                        $"[DEV_SEED] '{userName}' oluşturulamadı: {string.Join(", ", created.Errors.Select(e => e.Description))}");
                    return;
                }

                _logger.LogInformation($"[DEV_SEED] Kullanıcı oluşturuldu: {userName} ({role})");
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
