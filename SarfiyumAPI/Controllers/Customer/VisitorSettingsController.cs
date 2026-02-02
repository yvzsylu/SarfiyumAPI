using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/VisitorSettings")]
    [ApiController]
    [Authorize(Roles = "Admin,User")] // Sadece Tenant Sahibi (User) ve Admin yönetebilir. Viewer giremez.
    public class VisitorSettingsController : CustomBaseController
    {
        private readonly IVisitorSettingService _visitorService;

        public VisitorSettingsController(IVisitorSettingService visitorService)
        {
            _visitorService = visitorService;
        }

        // 1. ÖZEL AYAR EKLE / GÜNCELLE
        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] VisitorProductSettingDto model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _visitorService.UpsertSettingAsync(tenantId, model));
        }

        // 2. ÖZEL AYAR SİL
        // (Body yerine QueryString veya Route'dan alıyoruz, RESTful yapı için)
        [HttpDelete("delete/{visitorId}/{productId}")]
        public async Task<IActionResult> Delete(string visitorId, string productId)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _visitorService.DeleteSettingAsync(tenantId, visitorId, productId));
        }

        // 3. BİR KULLANICININ TÜM AYARLARINI GETİR
        [HttpGet("list/{visitorId}")]
        public async Task<IActionResult> GetSettings(string visitorId)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _visitorService.GetSettingsByVisitorAsync(tenantId, visitorId));
        }
    }
}