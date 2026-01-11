using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Categories")]
    [ApiController]
    [Authorize(Roles = "Admin,User")] // Yetki seviyesini kendine göre ayarla
    public class CategoryController : CustomBaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/customer/category/list
        [HttpGet("list")]
        public async Task<IActionResult> GetCategories()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _categoryService.GetCategoriesAsync(tenantId));
        }

        // PUT: api/customer/category/update/{id}
        // Body: { "name": "Yeni İsim" }
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateCategoryName(string id, [FromBody] UpdateCategoryNameDto model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _categoryService.UpdateCategoryNameAsync(tenantId, id, model));
        }
    }
}