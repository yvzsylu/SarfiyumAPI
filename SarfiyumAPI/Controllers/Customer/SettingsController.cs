using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using System.Security.Claims;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Settings")]
    [ApiController]
    // 1. ADIM: Sınıf seviyesine "Viewer" eklendi. Artık içeri girebilir.
    [Authorize(Roles = "Admin,User,Viewer")]
    public class SettingsController : CustomBaseController
    {
        private readonly ITenantSettingsService _settingsService;

        public SettingsController(ITenantSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // ---------------------------------------------------------------------
        // ✅ VIEWER'IN ERİŞEBİLDİĞİ ALAN (Okuma)
        // ---------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            // Token'dan TenantId'yi alıyoruz
            var tenantId = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            var result = await _settingsService.GetSettingsAsync(tenantId);

            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }

        // ---------------------------------------------------------------------
        // ⛔ VIEWER'IN ERİŞEMEDİĞİ ALAN (Yazma/Güncelleme)
        // ---------------------------------------------------------------------
        [HttpPut]
        [Authorize(Roles = "Admin,User")] // 🔥 Sadece Admin ve User değiştirebilir
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsDto dto)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            var result = await _settingsService.UpdateSettingsAsync(tenantId, dto);

            if (result.IsSuccess) return Ok(result);
            return BadRequest(result);
        }

        // ---------------------------------------------------------------------
        // ✅ YENİ EKLENEN TEMA GETİRME METODU
        // ---------------------------------------------------------------------
        [HttpGet("theme")]
        public async Task<IActionResult> GetThemeSettings()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            var result = await _settingsService.GetThemeSettingsAsync(tenantId);
            if (result.IsSuccess) return Ok(result);

            return BadRequest(result);
        }

        // ---------------------------------------------------------------------
        // ⛔ YENİ EKLENEN TEMA GÜNCELLEME METODU (Hata Düzeltildi: HttpPut yapıldı)
        // ---------------------------------------------------------------------
        [HttpPut("theme")]
        [Authorize(Roles = "Admin,User")] // Sadece yetkili olanlar yazabilir
        public async Task<IActionResult> UpdateThemeSettings([FromBody] UpdateThemeSettingsDto dto)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            var result = await _settingsService.UpdateThemeSettingsAsync(tenantId, dto);
            if (result.IsSuccess) return Ok(result);

            return BadRequest(result);
        }
    }
}