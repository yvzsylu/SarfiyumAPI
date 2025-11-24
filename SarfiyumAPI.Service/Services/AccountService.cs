using Microsoft.AspNetCore.Identity;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRepository<Tenant> _tenantRepository;

        public AccountService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRepository<Tenant> tenantRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantRepository = tenantRepository;
        }

        public async Task<ServiceResult<string>> CreateUserAsync(CreateUserDto model)
        {
            // 1. TENANT KONTROLÜ (Eğer ID gönderildiyse)
            if (!string.IsNullOrEmpty(model.TenantId))
            {
                var tenant = await _tenantRepository.GetByIdAsync(model.TenantId);
                if (tenant == null)
                    return ServiceResult<string>.Fail("Seçilen firma (Tenant) bulunamadı!", 404);
            }

            // 2. ROL KONTROLÜ (RoleId ile rolü buluyoruz)
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null)
                return ServiceResult<string>.Fail("Seçilen Rol sistemde bulunamadı!", 404);

            // 3. KULLANICI ADI KONTROLÜ
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return ServiceResult<string>.Fail("Bu kullanıcı adı zaten kullanımda!", 400);

            // 4. USER OLUŞTURMA
            AppUser user = new()
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                SecurityStamp = Guid.NewGuid().ToString(),

                // TenantId Dolu veya Null olabilir. 
                // Doluysa o firmaya bağlanır, Null ise Admin/Bağımsız olur.
                TenantId = model.TenantId
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ServiceResult<string>.Fail(errors, 400);
            }

            // 5. ROL ATAMA
            // Identity rol atarken "Role Name" ister. 
            // Biz RoleId ile bulduğumuz role nesnesinin Name özelliğini kullanıyoruz.
            await _userManager.AddToRoleAsync(user, role.Name!);

            return ServiceResult<string>.Success($"Kullanıcı başarıyla oluşturuldu ve {role.Name} rolü atandı.", 201);
        }

        public async Task<ServiceResult<string>> AssignRoleAsync(UserRoleDto model)
        {
            // ... (Mevcut kodların aynı kalacak) ...
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return ServiceResult<string>.Fail("User not found", 404);

            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (result.Succeeded)
                return ServiceResult<string>.Success("Role assigned successfully!", 200);

            var errors = result.Errors.Select(e => e.Description).ToList();
            return ServiceResult<string>.Fail(errors, 400);
        }
    }
}