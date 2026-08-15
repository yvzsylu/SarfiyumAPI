using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.Interfaces;

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
    public class PublicController : ControllerBase
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
        /// </summary>
        [HttpGet("tenant/{slug}")]
        public async Task<IActionResult> GetTenant(string slug)
        {
            var tenant = await _tenantService.GetPublicTenantAsync(slug);

            if (tenant == null) return NotFound();

            return Ok(tenant);
        }
    }
}
