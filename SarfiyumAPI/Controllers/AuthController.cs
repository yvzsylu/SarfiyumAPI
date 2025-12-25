using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

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
