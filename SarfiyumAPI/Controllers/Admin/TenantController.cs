using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Controllers.Admin
{
    // Admin Paneli endpoint'i olduğunu belirtmek için route'u özelleştirdim
    [Route("api/admin/[controller]")]
    [Tags("Admin/Tenant")]

    [ApiController]
    [Authorize(Roles = "Admin")] // Sadece Adminler erişebilir
    public class TenantController : CustomBaseController
    {
        private readonly ITenantService _tenantService;
        private readonly IAccountService _accountService; // Constructor'a eklemeyi unutma!
        private readonly RoleManager<IdentityRole> _roleManager;

        public TenantController(ITenantService tenantService, IAccountService accountService, RoleManager<IdentityRole> roleManager)
        {
            _tenantService = tenantService;
            _accountService = accountService;
            _roleManager = roleManager;
        }

        // GET: api/admin/Tenant
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return CreateActionResult(await _tenantService.GetAllAsync());
        }

        // GET: api/admin/Tenant/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return CreateActionResult(await _tenantService.GetByIdAsync(id));
        }

        // POST: api/admin/Tenant
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TenantDto model)
        {
            return CreateActionResult(await _tenantService.CreateAsync(model));
        }

        // PUT: api/admin/Tenant
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] TenantDto model)
        {
            return CreateActionResult(await _tenantService.UpdateAsync(model));
        }

        // DELETE: api/admin/Tenant/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return CreateActionResult(await _tenantService.DeleteAsync(id));
        }
        [HttpPost("create-with-user")]
        public async Task<IActionResult> CreateWithUser([FromBody] CreateTenantWithUserDto model)
        {
            // 1. "User" Rolünü Bul
            var userRole = await _roleManager.FindByNameAsync("User");
            if (userRole == null)
            {
                return BadRequest(ServiceResult<string>.Fail("Sistemde 'User' rolü bulunamadı.", 404));
            }

            // 2. Tenant DTO Hazırla (Mapping)
            var tenantDto = new TenantDto
            {
                Name = model.Name,
                TaxNumber = model.TaxNumber,
                PhoneNumber = model.PhoneNumber,
                Website = model.Website,
                // LogoUrl KALDIRILDI

                // Adres Mapping (Senin DTO yapına göre)
                City = model.City,
                District = model.District,
                FullAddress = model.FullAddress, // AddressDetail yok, FullAddress var
                ZipCode = model.ZipCode
            };

            // 3. Tenant Oluştur
            var tenantResult = await _tenantService.CreateAsync(tenantDto);

            if (!tenantResult.IsSuccess) return CreateActionResult(tenantResult);

            string newTenantId = tenantResult.Data;

            // 4. User DTO Hazırla
            var userDto = new CreateUserDto
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password,
                FullName = model.FullName,
                TenantId = newTenantId, // Yeni oluşan firma ID'si

                // Bulduğumuz "User" rolünün ID'sini atıyoruz
                RoleId = userRole.Id
            };

            // 5. Kullanıcı Oluştur
            var userResult = await _accountService.CreateUserAsync(userDto);

            if (!userResult.IsSuccess)
            {
                // Hata durumunda Rollback (Firma silinir)
                await _tenantService.DeleteAsync(newTenantId);
                return CreateActionResult(userResult);
            }

            return Ok(ServiceResult<string>.Success(newTenantId, 201));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusDto model)
        {
            return CreateActionResult(await _tenantService.UpdateStatusAsync(id, model.IsActive));
        }
    }
}
