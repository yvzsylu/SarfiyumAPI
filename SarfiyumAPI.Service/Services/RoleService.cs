using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleService(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<ServiceResult<List<RoleDto>>> GetAllRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();

            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name!
            }).ToList();

            return ServiceResult<List<RoleDto>>.Success(roleDtos);
        }

        public async Task<ServiceResult<string>> CreateRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (result.Succeeded)
                    return ServiceResult<string>.Success("Role created successfully!", 201);

                var errors = result.Errors.Select(e => e.Description).ToList();
                return ServiceResult<string>.Fail(errors, 400);
            }

            return ServiceResult<string>.Fail("Role already exists", 400);
        }
    }
}