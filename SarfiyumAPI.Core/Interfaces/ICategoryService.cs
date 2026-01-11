using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ICategoryService
    {
        // Tenant'a ait kategorileri OrderIndex sırasına göre getirir
        Task<ServiceResult<List<TenantCategoryDto>>> GetCategoriesAsync(string tenantId);

        // Sadece kategori ismini günceller (Sıra değişmez)
        Task<ServiceResult<string>> UpdateCategoryNameAsync(string tenantId, string categoryId, UpdateCategoryNameDto model);
    }
}