using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class SymbolCategoryService : ISymbolCategoryService
    {
        private readonly IRepository<SymbolCategory> _categoryRepository;
        private readonly IRepository<Symbol> _symbolRepository; // İlişki kontrolü için
        private readonly IDataContext _context;

        public SymbolCategoryService(
            IRepository<SymbolCategory> categoryRepository,
            IRepository<Symbol> symbolRepository,
            IDataContext context)
        {
            _categoryRepository = categoryRepository;
            _symbolRepository = symbolRepository;
            _context = context;
        }

        // 1. TÜMÜNÜ GETİR
        public async Task<ServiceResult<List<SymbolCategoryDto>>> GetAllAsync()
        {
            var categories = await _categoryRepository
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var dtos = categories.Select(c => new SymbolCategoryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();

            return ServiceResult<List<SymbolCategoryDto>>.Success(dtos);
        }

        // 2. ID İLE GETİR
        public async Task<ServiceResult<SymbolCategoryDto>> GetByIdAsync(string id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
                return ServiceResult<SymbolCategoryDto>.Fail("Kategori bulunamadı.", 404);

            return ServiceResult<SymbolCategoryDto>.Success(new SymbolCategoryDto
            {
                Id = category.Id,
                Name = category.Name
            });
        }

        // 3. EKLE
        public async Task<ServiceResult<string>> CreateAsync(CreateUpdateSymbolCategoryDto model)
        {
            // Aynı isimde kategori var mı?
            var exists = await _categoryRepository
                .Where(x => x.Name == model.Name && !x.IsDeleted)
                .AnyAsync();

            if (exists) return ServiceResult<string>.Fail("Bu kategori zaten mevcut.", 400);

            var category = new SymbolCategory
            {
                Name = model.Name,
                CreatedAt = DateTime.UtcNow,
                CreatedByName = "Admin"
            };

            await _categoryRepository.AddAsync(category);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success(category.Id, 201);
        }

        // 4. GÜNCELLE
        public async Task<ServiceResult<string>> UpdateAsync(string id, CreateUpdateSymbolCategoryDto model)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
                return ServiceResult<string>.Fail("Kategori bulunamadı.", 404);

            // İsim değişiyorsa, yeni isim başka bir kategoride var mı kontrol et
            if (category.Name != model.Name)
            {
                var exists = await _categoryRepository
                    .Where(x => x.Name == model.Name && x.Id != id && !x.IsDeleted)
                    .AnyAsync();

                if (exists) return ServiceResult<string>.Fail("Bu isimde başka bir kategori zaten var.", 400);
            }

            category.Name = model.Name;
            category.ModifiedAt = DateTime.UtcNow;
            category.ModifiedByName = "Admin";

            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Kategori güncellendi.", 200);
        }

        // 5. SİL (Soft Delete + İlişki Kontrolü)
        public async Task<ServiceResult<string>> DeleteAsync(string id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null || category.IsDeleted)
                return ServiceResult<string>.Fail("Kategori bulunamadı.", 404);

            // Kategoriye bağlı sembol var mı? Varsa sildirme.
            var hasSymbols = await _symbolRepository
                .Where(s => s.SymbolCategoryId == id && !s.IsDeleted)
                .AnyAsync();

            if (hasSymbols)
            {
                return ServiceResult<string>.Fail("Bu kategoriye bağlı semboller var. Önce sembolleri silin veya kategorisini değiştirin.", 400);
            }

            category.IsDeleted = true;
            category.RemovedAt = DateTime.UtcNow;
            category.RemovedByName = "Admin";

            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Kategori silindi.", 200);
        }
    }
}