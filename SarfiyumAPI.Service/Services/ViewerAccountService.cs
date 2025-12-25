using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Wrapper;
using System.Security.Claims;

namespace SarfiyumAPI.Service.Services
{
    public class ViewerAccountService : IViewerAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ViewerAccountService(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        // Yardımcı: Şu anki User'ın TenantId'sini al
        private string? GetCurrentTenantId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        }

        public async Task<ServiceResult<List<UserListDto>>> GetMyViewersAsync()
        {
            var tenantId = GetCurrentTenantId();
            if (string.IsNullOrEmpty(tenantId)) return ServiceResult<List<UserListDto>>.Fail("Tenant ID bulunamadı.", 400);

            // Sadece benim Tenant'ıma bağlı ve Rolü 'Viewer' olanları getir
            // Not: Performans için Identity'de Join yerine filtreleme yapıyoruz
            var users = await _userManager.Users
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();

            var viewerList = new List<UserListDto>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Viewer"))
                {
                    viewerList.Add(new UserListDto
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Username = user.UserName,
                        Email = user.Email,
                        IsActive = user.IsActive,
                        TenantId = user.TenantId,
                        Roles = new List<string> { "Viewer" }
                    });
                }
            }

            return ServiceResult<List<UserListDto>>.Success(viewerList);
        }

        public async Task<ServiceResult<string>> CreateViewerAsync(CreateUserDto model)
        {
            var tenantId = GetCurrentTenantId();
            if (string.IsNullOrEmpty(tenantId)) return ServiceResult<string>.Fail("Yetkisiz işlem.", 403);

            // 1. Kullanıcı Adı Kontrolü
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null) return ServiceResult<string>.Fail("Bu kullanıcı adı zaten kullanımda!", 400);

            // 2. User Oluştur (ZORUNLU OLARAK Current Tenant'a bağlanır)
            var user = new AppUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                TenantId = tenantId, // 🔥 Admin seçimi değil, token'dan gelen ID
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return ServiceResult<string>.Fail(result.Errors.First().Description, 400);

            // 3. Rol Ata (ZORUNLU OLARAK 'Viewer')
            await _userManager.AddToRoleAsync(user, "Viewer");

            return ServiceResult<string>.Success("İzleyici hesabı başarıyla oluşturuldu.", 201);
        }

        public async Task<ServiceResult<UserListDto>> GetViewerByIdAsync(string id)
        {
            var tenantId = GetCurrentTenantId();
            var user = await _userManager.FindByIdAsync(id);

            // Güvenlik: Sadece kendi tenant'ındaki kullanıcıyı görebilir
            if (user == null || user.TenantId != tenantId)
                return ServiceResult<UserListDto>.Fail("Kullanıcı bulunamadı.", 404);

            var isViewer = await _userManager.IsInRoleAsync(user, "Viewer");
            if (!isViewer) return ServiceResult<UserListDto>.Fail("Bu kullanıcı bir izleyici değil.", 403);

            return ServiceResult<UserListDto>.Success(new UserListDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.UserName,
                Email = user.Email,
                IsActive = user.IsActive,
                TenantId = user.TenantId
            });
        }

        public async Task<ServiceResult<string>> UpdateViewerAsync(UserUpdateDto model)
        {
            var tenantId = GetCurrentTenantId();
            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null || user.TenantId != tenantId)
                return ServiceResult<string>.Fail("Kullanıcı bulunamadı.", 404);

            user.FullName = model.FullName;
            user.UserName = model.Username;
            user.Email = model.Email;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return ServiceResult<string>.Fail(result.Errors.First().Description, 400);

            // Şifre Değişikliği Varsa
            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.Password);
            }

            return ServiceResult<string>.Success("Güncellendi.", 200);
        }

        public async Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive)
        {
            var tenantId = GetCurrentTenantId();
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.TenantId != tenantId)
                return ServiceResult<string>.Fail("Kullanıcı bulunamadı.", 404);

            user.IsActive = isActive;
            await _userManager.UpdateAsync(user);

            return ServiceResult<string>.Success("Durum güncellendi.", 200);
        }

        public async Task<ServiceResult<string>> DeleteViewerAsync(string id)
        {
            var tenantId = GetCurrentTenantId();
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.TenantId != tenantId)
                return ServiceResult<string>.Fail("Kullanıcı bulunamadı.", 404);

            // Admin veya Manager silmeye çalışırsa engelle, sadece Viewer silinebilir
            if (!await _userManager.IsInRoleAsync(user, "Viewer"))
                return ServiceResult<string>.Fail("Sadece izleyici hesaplarını silebilirsiniz.", 403);

            await _userManager.DeleteAsync(user);
            return ServiceResult<string>.Success("Silindi.", 200);
        }
    }
}