using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class VisitorSettingService : IVisitorSettingService
    {
        private readonly IRepository<VisitorProductSetting> _settingRepo;
        private readonly IDataContext _context; // SaveChanges için
        private readonly IMemoryCache _cache;

        public VisitorSettingService(
            IRepository<VisitorProductSetting> settingRepo,
            IRepository<UserActiveSession> sessionRepo,
            IDataContext context,
            IMemoryCache cache)
        {
            _settingRepo = settingRepo;
            _context = context;
            _cache = cache;
        }

        // ========================================================================
        // 1. HOSTED SERVICE & HUB METHODS (PERFORMANS KRİTİK)
        // ========================================================================

        public async Task<bool> HasSpecialSettingsAsync(string visitorId)
        {
            // Bu metot Hub OnConnected'da çağrılıyor, hızlı olmalı.
            // İstersen buraya da cache koyabilirsin ama DB sorgusu basit olduğu için şimdilik yeterli.
            return await _settingRepo.Where(x => x.VisitorId == visitorId && x.IsActive).AnyAsync();
        }

        public async Task<List<VisitorSessionDto>> GetOnlineVisitorsWithSettingsAsync(string tenantId)
        {
            // 🔥 1. ADIM: UZUN SÜRELİ CACHE KONTROLÜ
            string cacheKey = $"OnlineVisitorSettings_{tenantId}";

            if (_cache.TryGetValue(cacheKey, out List<VisitorSessionDto> cachedResult))
            {
                return cachedResult;
            }

            // --- BURADAN AŞAĞISI SADECE AYAR DEĞİŞTİĞİNDE ÇALIŞIR ---

            // 2. ADIM: ONLINE SORGUSUNU İPTAL ETTİK!
            // SignalR offline gruba atılan veriyi sessizce çöpe atar. SQL'i yormaya gerek yok.

            // 3. ADIM: SADECE TENANT'A AİT AKTİF ÖZEL AYARLARI ÇEK
            var settings = await _settingRepo
                .Where(x => x.TenantId == tenantId && x.IsActive)
                .ToListAsync();

            // Eğer firmanın hiçbir müşterisine tanımlı özel ayarı yoksa:
            if (!settings.Any())
            {
                var emptyList = new List<VisitorSessionDto>();

                // Cache süresini 5 saniyeden 1 SAATE çıkardık!
                // Zaten sen Upsert/Delete metodlarında ClearCache yaptığın için veri anında güncellenir.
                _cache.Set(cacheKey, emptyList, TimeSpan.FromHours(1));

                return emptyList;
            }

            // 4. ADIM: GRUPLAMA VE DTO ÇEVİRİMİ
            var result = settings
                .GroupBy(x => x.VisitorId)
                .Select(g => new VisitorSessionDto
                {
                    VisitorId = g.Key,
                    Settings = g.ToList()
                })
                .ToList();

            // 5. ADIM: SONUCU UZUN SÜRELİ CACHE'LE (1 SAAT)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        // ========================================================================
        // 2. CRUD METHODS (CONTROLLER)
        // ========================================================================

        public async Task<ServiceResult<string>> UpsertSettingAsync(string tenantId, VisitorProductSettingDto model)
        {


            var userExists = await _context.Set<AppUser>()
                .AnyAsync(u => u.Id == model.VisitorId && u.TenantId == tenantId);

            if (!userExists)
                return ServiceResult<string>.Fail($"Kullanıcı bulunamadı veya yetkiniz yok. (ID: {model.VisitorId})", 404);


            // 1. Bu Product gerçekten bu Tenant'a mı ait?
            var isProductValid = await _context.Set<TenantProduct>()
                .AnyAsync(p => p.Id == model.TenantProductId && p.TenantId == tenantId);

            if (!isProductValid)
                return ServiceResult<string>.Fail("Geçersiz ürün veya yetkisiz işlem.", 403);

            // 2. Bu Visitor (User) gerçekten bu Tenant'a mı bağlı?
            // AppUser tablosuna bakıyoruz
            var isVisitorValid = await _context.Set<AppUser>()
                .AnyAsync(u => u.Id == model.VisitorId && u.TenantId == tenantId);

            if (!isVisitorValid)
                return ServiceResult<string>.Fail("Bu kullanıcı sizin firmanıza ait değil.", 403);


            // Var olan ayarı bul
            var existing = await _settingRepo
                .Where(x => x.VisitorId == model.VisitorId && x.TenantProductId == model.TenantProductId && x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Güncelle
                existing.SpecialBuyMultiplier = model.SpecialBuyMultiplier;
                existing.SpecialSellMultiplier = model.SpecialSellMultiplier;
                existing.SpecialAddonAmount = model.SpecialAddonAmount;
                existing.IsActive = true;

                _settingRepo.Update(existing);
            }
            else
            {
                // Yeni Ekle
                var newSetting = new VisitorProductSetting
                {
                    TenantId = tenantId,
                    VisitorId = model.VisitorId,
                    TenantProductId = model.TenantProductId,
                    SpecialBuyMultiplier = model.SpecialBuyMultiplier,
                    SpecialSellMultiplier = model.SpecialSellMultiplier,
                    SpecialAddonAmount = model.SpecialAddonAmount,
                    IsActive = true
                };

                await _settingRepo.AddAsync(newSetting);
            }

            await _context.SaveChangesAsync();
            ClearCache(tenantId); // Cache'i temizle ki fiyatlar hemen yansısın

            return ServiceResult<string>.Success("Özel fiyat ayarı kaydedildi.", 200);
        }

        public async Task<ServiceResult<string>> DeleteSettingAsync(string tenantId, string visitorId, string productId)
        {
            var setting = await _settingRepo
                .Where(x => x.VisitorId == visitorId && x.TenantProductId == productId && x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (setting == null)
                return ServiceResult<string>.Fail("Ayar bulunamadı.", 404);

            _settingRepo.Remove(setting);
            await _context.SaveChangesAsync();
            ClearCache(tenantId);

            return ServiceResult<string>.Success("Özel fiyat ayarı kaldırıldı.", 200);
        }

        public async Task<ServiceResult<List<VisitorProductSetting>>> GetSettingsByVisitorAsync(string tenantId, string visitorId)
        {
            var list = await _settingRepo
                .Where(x => x.TenantId == tenantId && x.VisitorId == visitorId && x.IsActive)
                .Include(x => x.TenantProduct) // Ürün adını görmek için include gerekebilir
                .ToListAsync();

            return ServiceResult<List<VisitorProductSetting>>.Success(list);
        }

        public void ClearCache(string tenantId)
        {
            _cache.Remove($"OnlineVisitorSettings_{tenantId}");
        }
    }
}