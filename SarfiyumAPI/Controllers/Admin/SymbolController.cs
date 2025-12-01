using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    // Admin Paneli endpoint'i olduğunu belirtmek için route'u özelleştirdim
    [Route("api/admin/[controller]")]
    [Tags("Admin/Symbol")] // Swagger'da "Symbol" başlığı altında gruplanacak

    [ApiController]
    [Authorize(Roles = "Admin")] // Sadece Adminler erişebilir
    public class SymbolController : CustomBaseController
    {
        private readonly ISymbolService _symbolService;

        public SymbolController(ISymbolService symbolService)
        {
            _symbolService = symbolService;
        }

        // POST: api/admin/Symbol/sync-manual
        // Socket.IO verisini veya elindeki listeyi veritabanına eşitlemek için
        [HttpPost("sync-manual")]
        public async Task<IActionResult> SyncManual([FromBody] List<SyncSymbolRequest> data)
        {
            // CustomBaseController içindeki CreateActionResult metodunu kullanıyoruz
            return CreateActionResult(await _symbolService.SyncManualAsync(data));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return CreateActionResult(await _symbolService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return CreateActionResult(await _symbolService.GetByIdAsync(id));
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUpdateSymbolDto model)
        {
            return CreateActionResult(await _symbolService.CreateAsync(model));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateUpdateSymbolDto model)
        {
            return CreateActionResult(await _symbolService.UpdateAsync(id, model));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return CreateActionResult(await _symbolService.DeleteAsync(id));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            return CreateActionResult(await _symbolService.ToggleStatusAsync(id));
        }
    }
}