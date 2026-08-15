using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Products")]
    [ApiController]
    // 1. ADIM: Sınıf seviyesinde kapıyı Viewer'a açıyoruz.
    [Authorize(Roles = "Admin,User,Viewer")]
    public class ProductController : CustomBaseController
    {
        private readonly IMultiplierSettingsService _settingsService;

        public ProductController(IMultiplierSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // ---------------------------------------------------------------------
        // ✅ VIEWER'IN ERİŞEBİLDİĞİ TEK YER
        // ---------------------------------------------------------------------
        // GET api/customer/product/list
        [HttpGet("list")]
        public async Task<IActionResult> GetProducts()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            // Viewer buraya girebilir çünkü sınıf yetkisini (Admin,User,Viewer) kullanıyor.
            return CreateActionResult(await _settingsService.GetSettingsForUIAsync(tenantId));
        }

        // ---------------------------------------------------------------------
        // ⛔ AŞAĞIDAKİ TÜM METODLARDA VIEWER ENGELLENDİ
        // ---------------------------------------------------------------------

        // 2. KATEGORİLERİ LİSTELE
        [HttpGet("categories")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> GetCategories()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.GetCategoriesAsync(tenantId));
        }

        // 3. KATALOG LİSTELE
        [HttpGet("catalog-list")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> GetCatalog()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.GetAvailableCatalogItemsAsync());
        }

        // 4. YENİ ÜRÜN OLUŞTUR
        [HttpPost("create")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> CreateProduct([FromBody] CreateTenantProductDto model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.CreateProductAsync(tenantId, model));
        }

        // 5. TOPLU GÜNCELLEME
        [HttpPut("update-all")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> UpdateProducts([FromBody] List<UpdateTenantProductDto> model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.UpdateProductsAsync(tenantId, model));
        }

        // 6. TEKİL DURUM GÜNCELLEME
        [HttpPatch("status/{id}")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] bool isActive)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.UpdateProductStatusAsync(tenantId, id, isActive));
        }

        // 7. SİLME
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.DeleteProductAsync(tenantId, id));
        }

        // 8. SIRALAMA
        [HttpPatch("reorder")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> UpdateOrder([FromBody] List<ProductOrderRequestDto> model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.UpdateProductOrderAsync(tenantId, model));
        }

        // 9. WEB GÜNCELLEME
        [HttpPatch("toggle-web/{id}")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> ToggleWeb(string id)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.ToggleWebStatusAsync(tenantId, id));
        }

        // 10. MOBİL GÜNCELLEME
        [HttpPatch("toggle-mobile/{id}")]
        [Authorize(Roles = "Admin,User")] // 🔥 Viewer Engellendi
        public async Task<IActionResult> ToggleMobile(string id)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.ToggleMobileStatusAsync(tenantId, id));
        }
    }
}