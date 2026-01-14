using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ITenantSettingsService
    {
        Task<List<TenantProduct>> GetProductsAsync(string tenantId);

        // 2. UI: Yönetim Paneli Ürün Listesi
        Task<ServiceResult<List<TenantSettingResponseDto>>> GetSettingsForUIAsync(string tenantId);

        // 3. UI: Kategori Listesi (Dropdown'lar için) [YENİ]
        Task<ServiceResult<List<TenantProductCategoryDto>>> GetCategoriesAsync(string tenantId);

        // 4. UI: Yeni Ürün Ekleme [YENİ]
        Task<ServiceResult<string>> CreateProductAsync(string tenantId, CreateTenantProductDto model);


        // 6. UI: Tekil Durum Güncelleme (Aktif/Pasif)
        Task<ServiceResult<string>> UpdateProductStatusAsync(string tenantId, string productId, bool isActive);

        // 7. UI: Ürün Silme [YENİ]
        Task<ServiceResult<string>> DeleteProductAsync(string tenantId, string productId);

        Task<ServiceResult<string>> UpdateProductsAsync(string tenantId, List<UpdateTenantProductDto> updates);

        Task<ServiceResult<string>> UpdateProductOrderAsync(string tenantId, List<ProductOrderRequestDto> orderList);

        Task<ServiceResult<List<SystemCatalogDto>>> GetAvailableCatalogItemsAsync();

        // 8. Cache Temizleme
        void ClearCache(string tenantId);
    }
}
