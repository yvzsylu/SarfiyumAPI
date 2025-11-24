using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

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

        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
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
    }
}