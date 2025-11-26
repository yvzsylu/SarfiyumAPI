using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [Tags("Admin/Role")]

    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RoleController : CustomBaseController
    {
        private readonly IRoleService _roleService; // DEĞİŞTİ

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            return CreateActionResult(await _roleService.GetAllRolesAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto model)
        {
            return CreateActionResult(await _roleService.CreateRoleAsync(model));
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRole([FromBody] RoleDto model)
        {
            // RoleDto içinde Id ve Name olmalı
            return CreateActionResult(await _roleService.UpdateRoleAsync(model));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            return CreateActionResult(await _roleService.DeleteRoleAsync(id));
        }
    }
}