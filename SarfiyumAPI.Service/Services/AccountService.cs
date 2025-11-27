using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly IDataContext _context;


        public AccountService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IRepository<Tenant> tenantRepository,
            IDataContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _tenantRepository = tenantRepository;
            _context = context;
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





        // 1. TÜM KULLANICILARI GETİR (Tenant ve Rol Bilgileriyle)
        public async Task<ServiceResult<List<UserListDto>>> GetAllUsersAsync()
        {
            // Tüm kullanıcıları çekiyoruz
            var users = await _userManager.Users.ToListAsync();

            // Tenant adlarını hızlıca lookup yapmak için dictionary oluşturuyoruz
            // Sadece TenantId'si non-null olan kullanıcılar için Tenant adlarına bakıyoruz
            var tenantIds = users.Where(u => !string.IsNullOrEmpty(u.TenantId)).Select(u => u.TenantId).Distinct().ToList();

            // Yalnızca ilgili Tenant'ları veritabanından çekiyoruz
            var tenantsDict = await _tenantRepository
                .Where(t => tenantIds.Contains(t.Id) && t.IsDeleted == false)
                .ToDictionaryAsync(t => t.Id, t => t.Name);

            var userList = new List<UserListDto>();

            foreach (var user in users)
            {
                // Identity'den kullanıcının rollerini çekiyoruz
                var roles = await _userManager.GetRolesAsync(user);

                string? currentTenantName = null;

                // MANTIK HATASI DÜZELTME 1: Tenant Durumu Kontrolü
                if (string.IsNullOrEmpty(user.TenantId))
                {
                    // TenantId null ise, bu kullanıcı bir Admin'dir.
                    currentTenantName = "Sistem Yöneticisi";
                }
                else
                {
                    // MANTIK HATASI DÜZELTME 2: Tenant Adı Eşleştirmesi
                    // TenantId doluysa, Tenant adını lookup tablosundan al.
                    if (tenantsDict.TryGetValue(user.TenantId, out string? name))
                    {
                        currentTenantName = name;
                    }
                    else
                    {
                        // TenantId var ama Tenant tablosunda eşleşen yoksa (silinmiş/kayıp)
                        currentTenantName = "Bilinmeyen/Pasif Firma";
                    }
                }

                userList.Add(new UserListDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Username = user.UserName,
                    IsActive = user.IsActive,
                    TenantId = user.TenantId,
                    TenantName = currentTenantName, // Artık Admin veya Firma Adı burada
                    Roles = roles.ToList()
                });
            }

            return ServiceResult<List<UserListDto>>.Success(userList);
        }

        // UserService.cs içinde


        public async Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult<string>.Fail("Tenant not found", 404);

            user.IsActive = isActive;
            // Diğer alanlara dokunmuyoruz, sadece IsActive değişiyor.
            // ModifiedAt otomatik güncellenecek (EntityBase).

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return ServiceResult<string>.Fail(updateResult.Errors.First().Description, 500);
            }
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Status updated successfully", 200);
        }


        // UserService.cs içinde

        // 2. KULLANICININ MEVCUT ROLLERİNİ GETİR
        public async Task<ServiceResult<List<string>>> GetUserRolesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // TenantService'teki gibi Fail dönüşü
                return ServiceResult<List<string>>.Fail("User not found", 404);
            }

            var roles = await _userManager.GetRolesAsync(user);

            return ServiceResult<List<string>>.Success(roles.ToList());
        }

        // 3. KULLANICININ ROLLERİNİ GÜNCELLE
        public async Task<ServiceResult<bool>> UpdateUserRolesAsync(string userId, List<string> roleNames)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<bool>.Fail("User not found", 404);
            }

            // 1. Mevcut Roller
            var existingRoles = await _userManager.GetRolesAsync(user);

            // 2. Geçersiz Rol Kontrolü (Atanmaya çalışılan roller sistemde var mı?)
            var allSystemRoleNames = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var invalidRoles = roleNames.Where(r => !allSystemRoleNames.Contains(r)).ToList();

            if (invalidRoles.Any())
            {
                return ServiceResult<bool>.Fail($"Invalid role(s) found: {string.Join(", ", invalidRoles)}", 400);
            }

            // 3. Mevcut Rolleri Kaldır
            var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!removeResult.Succeeded)
            {
                return ServiceResult<bool>.Fail(removeResult.Errors.First().Description, 500);
            }

            // 4. Yeni Rolleri Ekle (roleNames boş olabilir, bu durumda rol atanmamış olur)
            var addResult = await _userManager.AddToRolesAsync(user, roleNames);
            if (!addResult.Succeeded)
            {
                // Hata durumunda rollback yapılması gerekir. Basitlik için burada hata döndürüyoruz.
                return ServiceResult<bool>.Fail(addResult.Errors.First().Description, 500);
            }

            return ServiceResult<bool>.Success(true);
        }


        public async Task<ServiceResult<string>> UpdateUserAsync(UserUpdateDto model)
        {
            // 1. KULLANICIYI BUL
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return ServiceResult<string>.Fail("Kullanıcı bulunamadı.", 404);
            }

            // 2. TENANT KONTROLÜ (Değişmişse veya yeni atanıyorsa)
            if (!string.IsNullOrEmpty(model.TenantId))
            {
                var tenant = await _tenantRepository.GetByIdAsync(model.TenantId);
                if (tenant == null)
                    return ServiceResult<string>.Fail("Seçilen firma (Tenant) bulunamadı!", 404);
            }

            // 3. ROL KONTROLÜ (Yeni Rol ID geçerli mi?)
            var newRole = await _roleManager.FindByIdAsync(model.RoleId);
            if (newRole == null)
                return ServiceResult<string>.Fail("Seçilen Rol sistemde bulunamadı!", 404);

            // 4. TEMEL BİLGİLERİ GÜNCELLE
            user.UserName = model.Username;
            user.Email = model.Email;
            user.FullName = model.FullName;
            user.TenantId = model.TenantId;
            user.IsActive = model.IsActive;

            // Kullanıcıyı güncelle
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult<string>.Fail(updateResult.Errors.First().Description, 400);
            }

            // 5. ŞİFRE GÜNCELLEME (Eğer dolu geldiyse)
            if (!string.IsNullOrEmpty(model.Password))
            {
                // Admin yetkisiyle şifre sıfırlama token'ı alıp şifreyi değiştiriyoruz
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (!passwordResult.Succeeded)
                {
                    return ServiceResult<string>.Fail("Şifre güncellenemedi: " + passwordResult.Errors.First().Description, 400);
                }
            }

            // 6. ROL GÜNCELLEME
            // Mevcut rolleri al
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Eğer kullanıcının rolü değişmişse (veya hiç rolü yoksa)
            if (!currentRoles.Contains(newRole.Name!))
            {
                // Eski rolleri sil
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Yeni rolü ata
                await _userManager.AddToRoleAsync(user, newRole.Name!);
            }

            return ServiceResult<string>.Success("Kullanıcı başarıyla güncellendi.", 200);
        }

        public async Task<ServiceResult<UserListDto>> GetUserByIdAsync(string id)
        {
            // 1. Kullanıcıyı Bul
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult<UserListDto>.Fail("Kullanıcı bulunamadı.", 404);
            }

            // 2. Rolleri Getir
            var roles = await _userManager.GetRolesAsync(user);

            // 3. Tenant İsmini Getir (Eğer varsa)
            string? tenantName = null;
            if (!string.IsNullOrEmpty(user.TenantId))
            {
                var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
                tenantName = tenant?.Name ?? "Bilinmeyen Firma";
            }
            else
            {
                tenantName = "Sistem Yöneticisi";
            }

            // 4. DTO'yu Oluştur
            var userDto = new UserListDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Username = user.UserName!,
                IsActive = user.IsActive,
                TenantId = user.TenantId,
                TenantName = tenantName,
                Roles = roles.ToList()
            };

            return ServiceResult<UserListDto>.Success(userDto);
        }


    }
}