using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<TenantProductCategory> _categoryRepository;
        private readonly IDataContext _context;

        // 🔥 1. TenantSettingsService'i buraya çağırıyoruz (Cache silmek için)
        private readonly IMultiplierSettingsService _tenantSettingsService;

        public CategoryService(
            IRepository<TenantProductCategory> categoryRepository,
            IDataContext context,
            IMultiplierSettingsService tenantSettingsService) // Constructor'a ekle
        {
            _categoryRepository = categoryRepository;
            _context = context;
            _tenantSettingsService = tenantSettingsService;
        }

        public async Task<ServiceResult<List<TenantCategoryDto>>> GetCategoriesAsync(string tenantId)
        {
            var categories = await _categoryRepository
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.OrderIndex)
                .ToListAsync();

            var dtos = categories.Select(c => new TenantCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                OrderIndex = c.OrderIndex
            }).ToList();

            return ServiceResult<List<TenantCategoryDto>>.Success(dtos);
        }

        public async Task<ServiceResult<string>> UpdateCategoryNameAsync(string tenantId, string categoryId, UpdateCategoryNameDto model)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);

            if (category == null)
                return ServiceResult<string>.Fail("Kategori bulunamadı.", 404);

            if (category.TenantId != tenantId)
                return ServiceResult<string>.Fail("Bu işlem için yetkiniz yok.", 403);

            // İsim güncelle
            category.Name = model.Name;

            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            // 🔥 2. KRİTİK NOKTA: Cache'i Temizle!
            // Bunu yapmazsan Worker Service 1 saat boyunca eski ismi yayınlamaya devam eder.
            // Çünkü ürünler bu kategorileri Include ederek cache'lenmiştir.
            _tenantSettingsService.ClearCache(tenantId);

            return ServiceResult<string>.Success("Kategori adı güncellendi.", 200);
        }
    }
}