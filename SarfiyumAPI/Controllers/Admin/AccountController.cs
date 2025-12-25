using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Service.Services;

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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _accountService.GetAllUsersAsync();
            return CreateActionResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{userId}/roles")]
        public async Task<IActionResult> GetRoles(string userId)
        {
            var result = await _accountService.GetUserRolesAsync(userId);
            return CreateActionResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{userId}/roles")]
        public async Task<IActionResult> AssignRoles(string userId, [FromBody] AssignRolesDto model)
        {
            var result = await _accountService.UpdateUserRolesAsync(userId, model.RoleNames);

            return CreateActionResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusDto model)
        {
            return CreateActionResult(await _accountService.UpdateStatusAsync(id, model.IsActive));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateDto model)
        {
            return CreateActionResult(await _accountService.UpdateUserAsync(model));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return CreateActionResult(await _accountService.GetUserByIdAsync(id));
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
        // DELETE: api/admin/Tenant/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return CreateActionResult(await _accountService.DeleteAsync(id));
        }
    }
}