using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Products")] // Swagger Tag'ini değiştirdim

    [ApiController]
    [Authorize(Roles = "Admin,User")]
    public class ProductController : CustomBaseController
    {
        private readonly ITenantSettingsService _settingsService;

        public ProductController(ITenantSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // 1. ÜRÜNLERİ LİSTELE
        // GET api/customer/product/list
        [HttpGet("list")]
        public async Task<IActionResult> GetProducts()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();
            var roles = User.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToList();

            return CreateActionResult(await _settingsService.GetSettingsForUIAsync(tenantId));
        }

        // 2. KATEGORİLERİ LİSTELE (Dropdown İçin)
        // GET api/customer/product/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.GetCategoriesAsync(tenantId));
        }

        // 3. YENİ ÜRÜN OLUŞTUR
        // POST api/customer/product/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateTenantProductDto model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.CreateProductAsync(tenantId, model));
        }

        // 4. TOPLU GÜNCELLEME (Çarpanlar ve İsimler)
        // PUT api/customer/product/update-all
        [HttpPut("update-all")]
        public async Task<IActionResult> UpdateProducts([FromBody] List<UpdateTenantProductDto> model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.UpdateProductsAsync(tenantId, model));
        }

        // 5. TEKİL DURUM GÜNCELLEME (Aktif/Pasif)
        // PATCH api/customer/product/status/{id}
        [HttpPatch("status/{id}")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] bool isActive)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.UpdateProductStatusAsync(tenantId, id, isActive));
        }

        // 6. SİLME
        // DELETE api/customer/product/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.DeleteProductAsync(tenantId, id));
        }

        [HttpPatch("reorder")]
        public async Task<IActionResult> UpdateOrder([FromBody] List<ProductOrderRequestDto> model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.UpdateProductOrderAsync(tenantId, model));
        }

        [HttpGet("catalog-list")]
        public async Task<IActionResult> GetCatalog()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _settingsService.GetAvailableCatalogItemsAsync());
        }
    }
}