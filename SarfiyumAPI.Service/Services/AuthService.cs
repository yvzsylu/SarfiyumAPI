using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities; // AppUser ve Tenant buradan geliyor
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.ValueObjects; // Address buradan geliyor
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Service.Services
{
    public class AuthService : IAuthService
    {
        // 1. DEĞİŞİKLİK: IdentityUser yerine AppUser kullanıyoruz
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IDataContext _context;

        // 2. DEĞİŞİKLİK: Tenant oluşturmak için Repository ekliyoruz
        private readonly IRepository<Tenant> _tenantRepository;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IDataContext context,
            IRepository<Tenant> tenantRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _tenantRepository = tenantRepository;
        }

        public async Task<ServiceResult<string>> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                if (!user.IsActive)
                {
                    // 403 Forbidden (Erişim engellendi)
                    return ServiceResult<string>.Fail("Hesabınız yönetici tarafından pasife alınmıştır.", 403);
                }

                if (!userRoles.Contains("Admin") && !string.IsNullOrEmpty(user.TenantId))
                {
                    // Firmayı veritabanından çek
                    var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);

                    // KONTROL: Tenant yoksa, silinmişse veya pasifse
                    if (tenant == null || tenant.IsDeleted || !tenant.IsActive)
                    {
                        return ServiceResult<string>.Fail("Bağlı olduğunuz firma hesabı aktif değildir veya silinmiştir. Lütfen yönetimle iletişime geçiniz.", 403);
                    }
                }

                // 1. Standart Claimler
                var authClaims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("userId", user.Id),
                    new Claim("email", user.Email ?? ""),
                    new Claim("fullName", user.FullName ?? "")
                };

                // 2. Tenant Bilgilerini Ekleme
                // Eğer TenantId doluysa (Admin değilse), firma bilgilerini çekip ekle
                if (!string.IsNullOrEmpty(user.TenantId))
                {
                    // TenantId claim'i ekle
                    authClaims.Add(new Claim("TenantId", user.TenantId));

                    // Tenant detaylarını Repository'den çekiyoruz
                    var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);

                    if (tenant != null)
                    {
                        // Firma Adı
                        authClaims.Add(new Claim("TenantName", tenant.Name));

                        // Firma Logosu (Eğer varsa)
                        if (!string.IsNullOrEmpty(tenant.LogoUrl))
                        {
                            authClaims.Add(new Claim("TenantLogo", tenant.LogoUrl));
                        }
                    }
                }

                // 3. Rolleri Ekle
                authClaims.AddRange(userRoles.Select(role => new Claim("roles", role)));

                var token = GetToken(authClaims);
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return ServiceResult<string>.Success(tokenString, 200);
            }

            return ServiceResult<string>.Fail("Invalid username or password!", 401);
        }

        public async Task<ServiceResult<string>> AddRoleAsync(string roleName)
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

        public async Task<ServiceResult<string>> AssignRoleAsync(UserRoleDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
                return ServiceResult<string>.Fail("User not found", 404);

            var result = await _userManager.AddToRoleAsync(user, model.Role);
            if (result.Succeeded)
                return ServiceResult<string>.Success("Role assigned successfully!", 200);

            var errors = result.Errors.Select(e => e.Description).ToList();
            return ServiceResult<string>.Fail(errors, 400);
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"]!)),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }
    }
}