using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SettingsController : CustomBaseController
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        // GET: api/admin/Settings
        // Tüm ayarları döner (Sayfa yüklenirken çağıracaksın)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return CreateActionResult(await _settingsService.GetAllSettingsAsync());
        }

        // PUT: api/admin/Settings/smtp
        // Sadece SMTP sekmesini kaydederken çağıracaksın
        [Authorize(Roles = "Admin")]
        [HttpPut("smtp")]
        public async Task<IActionResult> UpdateSmtp([FromBody] SmtpSettingsDto model)
        {
            return CreateActionResult(await _settingsService.UpdateSmtpSettingsAsync(model));
        }
    }
}