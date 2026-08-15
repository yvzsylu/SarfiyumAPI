using Microsoft.AspNetCore.Http;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ITenantService
    {
        Task<ServiceResult<List<TenantDto>>> GetAllAsync();
        Task<ServiceResult<TenantDto>> GetByIdAsync(string id);
        Task<ServiceResult<string>> CreateAsync(TenantDto model);
        Task<ServiceResult<string>> UpdateAsync(TenantDto model);
        Task<ServiceResult<string>> DeleteAsync(string id);

        Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive);
        // IFormFile kullanabilmek için: using Microsoft.AspNetCore.Http;
        Task<ServiceResult<string>> UploadLogoAsync(string tenantId, IFormFile file);

        // Yeni Cache'li metodumuz:
        Task<List<string>> GetActiveTenantIdsAsync();

        /// <summary>
        /// Girişsiz yayını AÇIK olan aktif tenant'ların id'leri. Yayıncı, public listeyi
        /// yalnızca bunlar için hesaplar; bayrağı kapalı tenant'lara sıfır ek maliyet.
        /// <see cref="GetActiveTenantIdsAsync"/> ile aynı abonelik filtresini uygular.
        /// </summary>
        Task<List<string>> GetPublicFeedTenantIdsAsync();

        /// <summary>
        /// Public slug'dan tenant id'sini çözer. Tenant yoksa, pasifse, aboneliği bitmişse
        /// ya da public yayını kapalıysa <c>null</c> döner — yani bu metot aynı zamanda
        /// yetki kontrolüdür.
        /// </summary>
        Task<string?> ResolvePublicTenantIdAsync(string slug);

        /// <summary>
        /// Public ekranın açılışta göstereceği tenant bilgisi (ad, logo, kategoriler).
        /// Erişilemez durumdaki tenant için <c>null</c> döner.
        /// </summary>
        Task<PublicTenantDto?> GetPublicTenantAsync(string slug);

        // Yeni Tenant eklendiğinde cache temizlemek için gerekli olabilir
        void ClearTenantCache();

    }
}
