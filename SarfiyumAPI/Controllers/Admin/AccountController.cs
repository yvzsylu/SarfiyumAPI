using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    // Route: api/admin/Account
    [Route("api/admin/[controller]")]
    [Tags("Admin/Account")]

    [ApiController]
    public class AccountController : CustomBaseController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // 1. KULLANICI OLUŞTURMA (Yeni Eklenen Kısım)
        // Admin, TenantId gönderirse o firmaya, göndermezse boşa (Admin) ekler.
        [Authorize(Roles = "Admin")]
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
        {
            return CreateActionResult(await _accountService.CreateUserAsync(model));
        }

        // 3. KULLANICIYA ROL ATAMA
        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] UserRoleDto model)
        {
            return CreateActionResult(await _accountService.AssignRoleAsync(model));
        }

        // NOT: Eğer "Login" işlemi herkes açıksa (ki öyledir),
        // Onu ya buraya [AllowAnonymous] ile eklemelisin 
        // ya da "Public" bir AccountController'da tutmalısın.
        // Eğer Login buradaysa şöyle ekle:

        /*
        [HttpPost("login")]
        [AllowAnonymous] // Herkes giriş yapabilmeli
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            return CreateActionResult(await _authService.LoginAsync(model));
        }
        */
    }
}