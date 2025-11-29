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

        // NOT: İleride "Tüm Sembolleri Listele" (GetAll) gibi ihtiyaçların olursa
        // Önce ISymbolService'e metodunu ekleyip sonra buraya [HttpGet] olarak ekleyebilirsin.
    }
}