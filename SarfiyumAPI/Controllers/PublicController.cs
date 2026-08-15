using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Controllers
{
    /// <summary>
    /// Girişsiz (public) fiyat ekranının kullandığı uç noktalar.
    ///
    /// ⚠️ Buradaki her şey ANONİM erişime açıktır. Yeni bir uç nokta eklemeden önce
    /// "bu bilgi mağaza vitrininde de görünür mü?" sorusu sorulmalıdır.
    /// </summary>
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class PublicController : CustomBaseController
    {
        private readonly ITenantService _tenantService;

        public PublicController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        /// <summary>
        /// Public ekranın açılışta çağırdığı uç nokta: firma adı, logo ve kategori başlıkları.
        ///
        /// Slug geçersizse, tenant pasifse, aboneliği bitmişse ya da public yayını kapalıysa
        /// 404 döner — hepsi için AYNI cevap verilir ki hangi slug'ın var olduğu sızmasın.
        ///
        /// ⚠️ Cevap <see cref="ServiceResult{T}"/> ile sarmalanır. İstemciler (mobil
        /// <c>BaseApiService</c>) gövdenin bu şekilde olmasını bekler ve gövdeyi
        /// <c>data</c> alanından okur; DTO'yu çıplak döndürmek istemcide sessizce boş
        /// veri olarak görünür.
        /// </summary>
        [HttpGet("tenant/{slug}")]
        public async Task<IActionResult> GetTenant(string slug)
        {
            var tenant = await _tenantService.GetPublicTenantAsync(slug);

            if (tenant == null)
            {
                return CreateActionResult(
                    ServiceResult<PublicTenantDto>.Fail("Firma bulunamadı.", 404));
            }

            return CreateActionResult(ServiceResult<PublicTenantDto>.Success(tenant, 200));
        }
    }
}
