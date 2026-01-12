using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Customer
{
    [Route("api/customer/[controller]")]
    [Tags("Customer/Categories")]
    [ApiController]
    // 🔥 DÜZELTME 1: Sınıf başındaki kısıtlamayı kaldırıp genel 'Authorize' yapıyoruz.
    // Böylece Token'ı olan herkes (Viewer dahil) sınıfa girebilir.
    [Authorize]
    public class CategoryController : CustomBaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: api/customer/category/list
        [HttpGet("list")]
        // 🔥 DÜZELTME 2: Listeleme işlemine "Viewer" iznini ekliyoruz.
        [Authorize(Roles = "Admin,User,Viewer")]
        public async Task<IActionResult> GetCategories()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _categoryService.GetCategoriesAsync(tenantId));
        }

        // PUT: api/customer/category/update/{id}
        // Body: { "name": "Yeni İsim" }
        [HttpPut("update/{id}")]
        // 🔥 DÜZELTME 3: Güncelleme işleminde "Viewer" YOK! Sadece yetkililer.
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> UpdateCategoryName(string id, [FromBody] UpdateCategoryNameDto model)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            return CreateActionResult(await _categoryService.UpdateCategoryNameAsync(tenantId, id, model));
        }
    }
}