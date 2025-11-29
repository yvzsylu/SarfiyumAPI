using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Service.Services
{
    public class TenantSettingsService : ITenantSettingsService
    {
        private readonly IRepository<TenantMultiplier> _multiplierRepository; // 🔥 Repository Eklendi
        private readonly IDataContext _context;
        private readonly IMemoryCache _cache;

        public TenantSettingsService(
            IRepository<TenantMultiplier> multiplierRepository,
            IDataContext context,
            IMemoryCache cache)
        {
            _multiplierRepository = multiplierRepository;
            _context = context;
            _cache = cache;
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
    }
}