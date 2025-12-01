using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class TenantSettingsService : ITenantSettingsService
    {
        private readonly IRepository<TenantMultiplier> _multiplierRepository;
        private readonly IRepository<Symbol> _symbolRepository;// 🔥 Repository Eklendi
        private readonly IDataContext _context;
        private readonly IMemoryCache _cache;

        public TenantSettingsService(
            IRepository<TenantMultiplier> multiplierRepository,
            IDataContext context,
            IMemoryCache cache,
            IRepository<Symbol> symbolRepository)
        {
            _multiplierRepository = multiplierRepository;
            _context = context;
            _cache = cache;
            _symbolRepository = symbolRepository;
        }

        public async Task<Dictionary<string, TenantMultiplier>> GetMultipliersAsync(string tenantId)
        {
            string cacheKey = $"Multiplier_{tenantId}";

            // 1. Cache Kontrolü
            if (!_cache.TryGetValue(cacheKey, out Dictionary<string, TenantMultiplier> multipliers))
            {
                // 2. Repository üzerinden veriyi çek
                // Sembol ismine ihtiyacımız olduğu için .Include(x => x.Symbol) yapıyoruz.

                // NOT: IRepository implementasyonunun IQueryable döndürdüğünü varsayıyoruz (TenantService örneğindeki Where kullanımı gibi)
                var list = await _multiplierRepository
                    .Where(tm => tm.TenantId == tenantId && tm.IsActive)
                    .Include(tm => tm.Symbol) // Symbol tablosunu JOIN'le
                    .ToListAsync();

                // 3. Dictionary'e çevir (Key: "ALTIN", Value: Ayarlar)
                multipliers = list.ToDictionary(x => x.Symbol.Name, x => x);

                // 4. Cache Ayarları
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1)); // 1 saat kullanılmazsa sil

                _cache.Set(cacheKey, multipliers, cacheOptions);
            }

            return multipliers;
        }

        public void ClearCache(string tenantId)
        {
            _cache.Remove($"Multiplier_{tenantId}");
        }

        // ... Mevcut GetMultipliersAsync ve ClearCache metodları kalsın ...

        // 1. EKRANA VERİ BASMA (LEFT JOIN MANTIĞI)
        public async Task<ServiceResult<List<TenantSettingResponseDto>>> GetSettingsForUIAsync(string tenantId)
        {
            // A. Tüm Aktif Sembolleri Çek (Sol Tablo)
            // Sembolleri Repository'den çekmelisin. Constructor'a IRepository<Symbol> eklemeyi unutma!
            // Eğer yoksa _context üzerinden çekebilirsin ama repository pattern daha doğru.
            // Varsayım: IRepository<Symbol> _symbolRepository Constructor'a eklendi.

            var allSymbols = await _symbolRepository
                .Where(s => s.IsActive && !s.IsDeleted)
                .Include(s => s.Category)
                .ToListAsync();

            // B. Tenant'ın Kayıtlı Ayarlarını Çek (Sağ Tablo)
            var tenantSettings = await _multiplierRepository
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .ToListAsync();

            // C. Birleştirme (Mapping)
            var resultList = new List<TenantSettingResponseDto>();

            foreach (var sym in allSymbols)
            {
                // Tenant'ın bu sembol için ayarı var mı?
                var setting = tenantSettings.FirstOrDefault(x => x.SymbolId == sym.Id);

                resultList.Add(new TenantSettingResponseDto
                {
                    SymbolId = sym.Id,
                    SymbolName = sym.Name,
                    CategoryName = sym.Category?.Name ?? "Genel",

                    // Ayar varsa onu koy, yoksa VARSAYILAN değerleri koy
                    BuyMultiplier = setting != null ? setting.BuyMultiplier : 1.0000m,
                    SellMultiplier = setting != null ? setting.SellMultiplier : 1.0000m,
                    AddonAmount = setting != null ? setting.AddonAmount : 0.0000m
                });
            }

            // Kategoriye göre sırala
            var orderedList = resultList.OrderBy(x => x.CategoryName).ThenBy(x => x.SymbolName).ToList();

            return ServiceResult<List<TenantSettingResponseDto>>.Success(orderedList);
        }

        // 2. AYARLARI KAYDETME
        public async Task<ServiceResult<string>> UpdateSettingsAsync(string tenantId, List<UpdateTenantSettingDto> updates)
        {
            // Tenant'ın mevcut ayarlarını çek
            var existingSettings = await _multiplierRepository
                .Where(t => t.TenantId == tenantId)
                .ToListAsync();

            foreach (var updateItem in updates)
            {
                var targetEntity = existingSettings.FirstOrDefault(x => x.SymbolId == updateItem.SymbolId);

                if (targetEntity != null)
                {
                    // UPDATE: Zaten kayıt varsa güncelle
                    targetEntity.BuyMultiplier = updateItem.BuyMultiplier;
                    targetEntity.SellMultiplier = updateItem.SellMultiplier;
                    targetEntity.AddonAmount = updateItem.AddonAmount;
                    targetEntity.ModifiedAt = DateTime.UtcNow;

                    _multiplierRepository.Update(targetEntity);
                }
                else
                {
                    // INSERT: Kayıt yoksa yeni oluştur
                    var newEntity = new TenantMultiplier
                    {
                        TenantId = tenantId,
                        SymbolId = updateItem.SymbolId,
                        BuyMultiplier = updateItem.BuyMultiplier,
                        SellMultiplier = updateItem.SellMultiplier,
                        AddonAmount = updateItem.AddonAmount,
                        IsActive = true, // Default aktif
                        CreatedAt = DateTime.UtcNow
                    };

                    await _multiplierRepository.AddAsync(newEntity);
                }
            }

            // Toplu Kaydet
            await _context.SaveChangesAsync();

            // 🔥 KRİTİK: Cache temizle ki Dashboard'daki fiyatlar anında değişsin
            ClearCache(tenantId);

            return ServiceResult<string>.Success("Ayarlar başarıyla güncellendi.", 200);
        }
    }
}