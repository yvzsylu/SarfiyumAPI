using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Service.Services;
using System.Security.Claims; // User.FindFirst için

namespace SarfiyumAPI.Controllers
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Profile")]

    [ApiController]
    [Authorize] // Giriş yapmış herkes (Admin veya User) erişebilir
    public class ProfileController : CustomBaseController
    {
        private readonly IProfileService _profileService;
        private readonly ITenantService _tenantService; // Inject et

        public ProfileController(IProfileService profileService, ITenantService tenantService   )
        {
            _profileService = profileService;
            _tenantService = tenantService;
        }

        // GET: api/Profile/info
        [HttpGet("info")]
        public async Task<IActionResult> GetProfileInfo()
        {
            // Token'dan UserId'yi güvenli şekilde alıyoruz
            var userId = User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Kimlik doğrulama hatası." });
            }

            return CreateActionResult(await _profileService.GetProfileInfoAsync(userId));
        }

        [HttpPost("upload-logo")]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            // Token'dan TenantId'yi al
            var tenantId = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(tenantId))
                return BadRequest("Bu işlem için bir firmaya bağlı olmalısınız.");

            var result = await _tenantService.UploadLogoAsync(tenantId, file);
            return CreateActionResult(result);
        }
    }
}