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

        public async Task<ServiceResult<List<SymbolDto>>> GetAllAsync()
        {
            // Repository'deki Where metodunu kullanarak IQueryable alıyoruz.
            // Bu sayede .Include() ekleyebiliyoruz.
            var symbols = await _symbolRepository
                .Where(x => !x.IsDeleted) // IQueryable döner
                .Include(s => s.Category) // Category tablosunu JOIN'le
                .OrderBy(s => s.Category.Name)
                .ThenBy(s => s.Name)
                .ToListAsync(); // Veritabanına sorguyu şimdi at

            // Mapping
            var dtos = symbols.Select(s => new SymbolDto
            {
                Id = s.Id,
                Name = s.Name,
                CategoryId = s.SymbolCategoryId,
                CategoryName = s.Category?.Name ?? "-",
                IsActive = s.IsActive
            }).ToList();

            return ServiceResult<List<SymbolDto>>.Success(dtos);
        }

        // 2. YENİ SEMBOL EKLEME
        public async Task<ServiceResult<string>> CreateAsync(CreateUpdateSymbolDto model)
        {
            // Aynı isimde var mı? (Repository Where kullanarak)
            var exists = await _symbolRepository
                .Where(x => x.Name == model.Name && !x.IsDeleted)
                .AnyAsync();

            if (exists) return ServiceResult<string>.Fail("Bu sembol zaten mevcut.", 400);

            var symbol = new Symbol
            {
                Name = model.Name,
                SymbolCategoryId = model.CategoryId,
                IsActive = model.IsActive,
                CreatedByName = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            await _symbolRepository.AddAsync(symbol);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success(symbol.Id, 201);
        }

        // 3. SEMBOL GÜNCELLEME
        public async Task<ServiceResult<string>> UpdateAsync(string id, CreateUpdateSymbolDto model)
        {
            var symbol = await _symbolRepository.GetByIdAsync(id);
            if (symbol == null || symbol.IsDeleted)
                return ServiceResult<string>.Fail("Sembol bulunamadı.", 404);

            symbol.Name = model.Name;
            symbol.SymbolCategoryId = model.CategoryId;
            symbol.IsActive = model.IsActive;
            symbol.ModifiedAt = DateTime.UtcNow;
            symbol.ModifiedByName = "Admin";

            _symbolRepository.Update(symbol);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Sembol güncellendi.", 200);
        }

        // 4. SEMBOL SİLME (Soft Delete)
        public async Task<ServiceResult<string>> DeleteAsync(string id)
        {
            var symbol = await _symbolRepository.GetByIdAsync(id);
            if (symbol == null)
                return ServiceResult<string>.Fail("Sembol bulunamadı.", 404);

            symbol.IsDeleted = true;
            symbol.RemovedAt = DateTime.UtcNow;
            symbol.RemovedByName = "Admin";

            _symbolRepository.Update(symbol);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Sembol silindi.", 200);
        }

        // 5. DURUM DEĞİŞTİRME (Aktif/Pasif)
        public async Task<ServiceResult<string>> ToggleStatusAsync(string id)
        {
            var symbol = await _symbolRepository.GetByIdAsync(id);
            if (symbol == null)
                return ServiceResult<string>.Fail("Sembol bulunamadı.", 404);

            symbol.IsActive = !symbol.IsActive;
            _symbolRepository.Update(symbol);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success($"Durum değiştirildi: {(symbol.IsActive ? "Aktif" : "Pasif")}", 200);
        }

        public async Task<ServiceResult<SymbolDto>> GetByIdAsync(string id)
        {
            // Repository'nin 'Where' metodunu kullanıyoruz ki 'Include' yapabilelim.
            // Standart GetByIdAsync genelde Include desteklemez.
            var symbol = await _symbolRepository
                .Where(x => x.Id == id && !x.IsDeleted)
                .Include(s => s.Category) // Kategori ismini de görelim
                .FirstOrDefaultAsync();

            if (symbol == null)
                return ServiceResult<SymbolDto>.Fail("Sembol bulunamadı.", 404);

            // DTO Mapping
            var dto = new SymbolDto
            {
                Id = symbol.Id,
                Name = symbol.Name,
                CategoryId = symbol.SymbolCategoryId,
                CategoryName = symbol.Category?.Name ?? "-",
                IsActive = symbol.IsActive
            };

            return ServiceResult<SymbolDto>.Success(dto);
        }


    }
}