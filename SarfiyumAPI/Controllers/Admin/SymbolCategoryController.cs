using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    // Admin Paneli endpoint'i olduğunu belirtmek için route'u özelleştirdim
    [Route("api/admin/[controller]")]
    [Tags("Admin/SymbolCategory")] // Swagger'da "Symbol" başlığı altında gruplanacak

    [ApiController]
    [Authorize(Roles = "Admin")] // Sadece Adminler erişebilir
    public class SymbolCategoryController : CustomBaseController
    {
        private readonly ISymbolCategoryService _symbolCategoryService;

        public SymbolCategoryController(ISymbolCategoryService categoryService)
        {
            _symbolCategoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return CreateActionResult(await _symbolCategoryService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return CreateActionResult(await _symbolCategoryService.GetByIdAsync(id));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUpdateSymbolCategoryDto model)
        {
            return CreateActionResult(await _symbolCategoryService.CreateAsync(model));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CreateUpdateSymbolCategoryDto model)
        {
            return CreateActionResult(await _symbolCategoryService.UpdateAsync(id, model));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return CreateActionResult(await _symbolCategoryService.DeleteAsync(id));
        }
    }
}