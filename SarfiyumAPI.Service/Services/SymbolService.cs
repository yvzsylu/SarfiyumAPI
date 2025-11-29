using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class SymbolService : ISymbolService
    {
        private readonly IRepository<Symbol> _symbolRepository;
        private readonly IRepository<SymbolCategory> _categoryRepository;
        private readonly IDataContext _context;

        public SymbolService(
            IRepository<Symbol> symbolRepository,
            IRepository<SymbolCategory> categoryRepository,
            IDataContext context)
        {
            _symbolRepository = symbolRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        public async Task<ServiceResult<string>> SyncManualAsync(List<SyncSymbolRequest> requestData)
        {
            if (requestData == null || !requestData.Any())
                return ServiceResult<string>.Fail("Veri bulunamadı.", 400);

            // 1. KATEGORİLERİ SENKRONİZE ET
            // ----------------------------------------------------------------

            // Gelen istekteki benzersiz kategorileri bul
            var distinctCategories = requestData
                .Select(x => x.Category)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            // Mevcut kategorileri çek (Tracking açık olsun ki bellekte işlem yapalım)
            var existingCategories = await _categoryRepository.Where(x => !x.IsDeleted).ToListAsync();

            // Listeyi bellekte güncel tutmak için yeni eklenenleri de buraya atacağız
            var allCategoriesMap = new List<SymbolCategory>(existingCategories);

            foreach (var catName in distinctCategories)
            {
                // Bellekteki listede bu kategori var mı?
                var existingCat = allCategoriesMap.FirstOrDefault(c => c.Name == catName);

                if (existingCat == null)
                {
                    // YOKSA: Yeni oluştur
                    var newCat = new SymbolCategory
                    {
                        // EntityBase ID'yi otomatik oluşturur (Guid)
                        Name = catName,
                        CreatedByName = "ManualSync",
                        CreatedAt = DateTime.UtcNow,
                    };

                    await _categoryRepository.AddAsync(newCat);

                    // Listeye ekle ki birazdan sembolleri buna bağlayabilelim
                    allCategoriesMap.Add(newCat);
                }
            }

            // Kategorilerin ID'lerinin oluşması/garanti olması için bir save yapabiliriz
            // Ama Guid kullandığın için AddAsync anında ID oluşmuştur, zorunlu değil ama güvenli.
            await _context.SaveChangesAsync();


            // 2. SEMBOLLERİ SENKRONİZE ET
            // ----------------------------------------------------------------

            var existingSymbols = await _symbolRepository.Where(x => !x.IsDeleted).ToListAsync();
            int addedCount = 0;
            int updatedCount = 0;

            foreach (var item in requestData)
            {
                // Bu item hangi kategoriye ait? (Yukarıdaki güncel listeden buluyoruz)
                var categoryEntity = allCategoriesMap.FirstOrDefault(c => c.Name == item.Category);

                // Eğer kategori bir şekilde yoksa (çok düşük ihtimal), bu kaydı atla
                if (categoryEntity == null) continue;

                var existingSymbol = existingSymbols.FirstOrDefault(s => s.Name == item.Symbol);

                if (existingSymbol == null)
                {
                    // --- INSERT (EKLEME) ---
                    var newSymbol = new Symbol
                    {
                        Name = item.Symbol,
                        SymbolCategoryId = categoryEntity.Id, // Bağlantıyı kurduk
                        IsActive = true,
                        CreatedByName = "ManualSync",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _symbolRepository.AddAsync(newSymbol);
                    addedCount++;
                }
                else
                {
                    // --- UPDATE (GÜNCELLEME) ---
                    bool isChanged = false;

                    // Kategori değişmiş mi?
                    if (existingSymbol.SymbolCategoryId != categoryEntity.Id)
                    {
                        existingSymbol.SymbolCategoryId = categoryEntity.Id;
                        isChanged = true;
                    }

                    // (Not: Name değişimi kontrol edilmiyor çünkü Name bizim eşleştirme anahtarımız)

                    if (isChanged)
                    {
                        existingSymbol.ModifiedByName = "ManualSync";
                        existingSymbol.ModifiedAt = DateTime.UtcNow;

                        _symbolRepository.Update(existingSymbol);
                        updatedCount++;
                    }
                }
            }

            // Değişiklik varsa kaydet
            if (addedCount > 0 || updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return ServiceResult<string>.Success($"Sync Tamamlandı: {addedCount} yeni eklendi, {updatedCount} güncellendi.", 200);
        }
    }
}