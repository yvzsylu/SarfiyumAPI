using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Multiplier")]

    [ApiController]
    [Authorize]
    public class MultiplierController : CustomBaseController
    {
        private readonly ITenantSettingsService _settingsService;

        public MultiplierController(ITenantSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // GET api/settings/multipliers
        [HttpGet("multipliers")]
        public async Task<IActionResult> GetMultipliers()
        {
            // Token'dan TenantId'yi otomatik alıyoruz (Güvenlik)
            var tenantId = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized("Tenant ID bulunamadı.");

            return CreateActionResult(await _settingsService.GetSettingsForUIAsync(tenantId));
        }

        // POST api/settings/multipliers
        [HttpPost("update-multipliers")]
        public async Task<IActionResult> UpdateMultipliers([FromBody] List<UpdateTenantSettingDto> model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return Unauthorized("Tenant ID bulunamadı.");

            return CreateActionResult(await _settingsService.UpdateSettingsAsync(tenantId, model));
        }
    }
}
