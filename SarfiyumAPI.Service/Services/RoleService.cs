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

        public async Task<ServiceResult<string>> CreateRoleAsync(CreateRoleDto model)
        {
            // model.Name olarak erişiyoruz
            if (!await _roleManager.RoleExistsAsync(model.Name))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(model.Name));

                if (result.Succeeded)
                    return ServiceResult<string>.Success("Role created successfully!", 201);

                var errors = result.Errors.Select(e => e.Description).ToList();
                return ServiceResult<string>.Fail(errors, 400);
            }

            return ServiceResult<string>.Fail("Role already exists", 400);
        }

        // YENİ EKLENEN METOT
        public async Task<ServiceResult<string>> UpdateRoleAsync(RoleDto model)
        {
            // 1. Güncellenecek rolü ID ile bul
            var role = await _roleManager.FindByIdAsync(model.Id);
            if (role == null)
            {
                return ServiceResult<string>.Fail("Role not found", 404);
            }

            // 2. Eğer isim değişmişse güncelle
            if (role.Name != model.Name)
            {
                // (Opsiyonel) Aynı isimde başka rol var mı kontrolü
                var roleExists = await _roleManager.RoleExistsAsync(model.Name);
                if (roleExists)
                {
                    return ServiceResult<string>.Fail("Role name already exists", 400);
                }

                role.Name = model.Name;

                // Identity, NormalizedName gibi alanları UpdateAsync ile otomatik halleder
                var result = await _roleManager.UpdateAsync(role);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ServiceResult<string>.Fail(errors, 400);
                }
            }

            return ServiceResult<string>.Success("Role updated successfully", 200);
        }

        public async Task<ServiceResult<string>> DeleteRoleAsync(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return ServiceResult<string>.Fail("Role not found", 404);
            }

            // (Opsiyonel Güvenlik) Admin rolünün silinmesini engelleyebilirsin
            if (role.Name == "Admin")
            {
                return ServiceResult<string>.Fail("Admin rolü silinemez!", 400);
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return ServiceResult<string>.Fail("Delete failed", 400);
            }

            return ServiceResult<string>.Success("Role deleted successfully", 200);
        }
    }
}