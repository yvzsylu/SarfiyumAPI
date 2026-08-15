using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : CustomBaseController
    {

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            return CreateActionResult(await _authService.LoginAsync(model));
        }

        [AllowAnonymous] // Şifresini unutan kişi login değildir
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto model)
        {
            return CreateActionResult(await _authService.ResetPasswordAsync(model.Email));
        }

        /// <summary>
        /// Oturumu sunucu tarafında kapatır. Bu endpoint yokken çıkış yalnızca istemcide
        /// yapılıyor, <c>UserActiveSessions</c> satırı kalıyor ve kullanıcı sunucu tarafında
        /// "hâlâ online" görünüyordu (AGENTS.md G10).
        ///
        /// Gövde almaz: kimin hangi oturumu kapattığı token'daki claim'lerden okunur,
        /// böylece istemci başka bir kullanıcının oturumunu kapatamaz.
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst("userId")?.Value;
            var sessionId = User.FindFirst("SessionId")?.Value;
            var clientSourceStr = User.FindFirst("ClientSource")?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId) ||
                !Enum.TryParse<ClientSource>(clientSourceStr, out var clientSource))
            {
                return CreateActionResult(
                    ServiceResult<string>.Fail("Oturum bilgisi okunamadı.", 401));
            }

            return CreateActionResult(
                await _authService.LogoutAsync(userId, clientSource, sessionId, tenantId));
        }

        //[HttpGet("check")]
        //public IActionResult Check()
        //{
        //    return Ok(new
        //    {
        //        Status = "Sistem Aktif",
        //        Version = "1.0.1",
        //        ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //        Message = "GitHub Actions üzerinden başarıyla güncellendi!"
        //    });
        //}

    }




}
