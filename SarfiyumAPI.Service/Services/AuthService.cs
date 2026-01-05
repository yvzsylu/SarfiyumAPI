using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.ValueObjects;
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
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IDataContext _context;
        private readonly IEmailService _emailService;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<UserLoginHistory> _loginHistoryRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<UserActiveSession> _activeSessionRepo;
        private readonly ISessionNotificationService _sessionNotificationService;

        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IDataContext context,
            IRepository<Tenant> tenantRepository,
            IEmailService emailService,
            IRepository<UserLoginHistory> loginHistoryRepo,
            IHttpContextAccessor httpContextAccessor,
            IRepository<UserActiveSession> activeSessionRepo,
            ISessionNotificationService sessionNotificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _tenantRepository = tenantRepository;
            _emailService = emailService;
            _loginHistoryRepo = loginHistoryRepo;
            _httpContextAccessor = httpContextAccessor;
            _activeSessionRepo = activeSessionRepo;
            _sessionNotificationService = sessionNotificationService;
        }

        public async Task<ServiceResult<string>> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                if (!user.IsActive) return ServiceResult<string>.Fail("Hesabınız pasif.", 403);

                if (!userRoles.Contains("Admin") && !string.IsNullOrEmpty(user.TenantId))
                {
                    var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
                    if (tenant == null || !tenant.IsActive) return ServiceResult<string>.Fail("Firma aktif değil.", 403);
                    if (!tenant.SubscriptionEndDate.HasValue) return ServiceResult<string>.Fail("Abonelik yok.", 403);
                    if (tenant.SubscriptionEndDate.Value < DateTime.UtcNow) return ServiceResult<string>.Fail("Abonelik doldu.", 403);
                }

                // 1. Session İşlemleri
                var newSessionId = Guid.NewGuid().ToString();

                var existingSession = await _activeSessionRepo
                    .Where(x => x.UserId == user.Id && x.ClientSource == model.ClientSource)
                    .FirstOrDefaultAsync();

                if (existingSession != null)
                {
                    existingSession.SessionId = newSessionId;
                    existingSession.LastLoginDate = DateTime.UtcNow;
                    _activeSessionRepo.Update(existingSession);
                }
                else
                {
                    await _activeSessionRepo.AddAsync(new UserActiveSession
                    {
                        UserId = user.Id,
                        ClientSource = model.ClientSource,
                        SessionId = newSessionId,
                        LastLoginDate = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();

                // 2. Token Claims
                var authClaims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("userId", user.Id),
                    new Claim("email", user.Email ?? ""),
                    new Claim("fullName", user.FullName ?? ""),
                    new Claim("SessionId", newSessionId),
                    new Claim("ClientSource", model.ClientSource.ToString())
                };

                if (!string.IsNullOrEmpty(user.TenantId))
                {
                    authClaims.Add(new Claim("tenantId", user.TenantId));
                    var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
                    if (tenant != null) authClaims.Add(new Claim("tenantName", tenant.Name));
                }
                authClaims.AddRange(userRoles.Select(role => new Claim("roles", role)));

                DateTime tokenExpiration = userRoles.Any(r => r.Equals("Viewer", StringComparison.OrdinalIgnoreCase))
                    ? DateTime.UtcNow.AddMinutes(60)
                    : DateTime.UtcNow.AddMonths(6);

                var token = GetToken(authClaims, tokenExpiration);
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                await LogLoginInternalAsync(user.Id, user.UserName);

                // 🔥 DEĞİŞİKLİK: Interface üzerinden Hub'ı tetikliyoruz
                // API katmanında yazdığımız SignalRSessionService çalışacak
                await _sessionNotificationService.SendForceCheckSession(user.Id, newSessionId, model.ClientSource.ToString());

                return ServiceResult<string>.Success(tokenString, 200);
            }
            return ServiceResult<string>.Fail("Kullanıcı adı veya şifre hatalı!", 401);
        }
        private JwtSecurityToken GetToken(List<Claim> authClaims, DateTime expires)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: expires,
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }

        public async Task<ServiceResult<string>> ResetPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return ServiceResult<string>.Fail("Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.", 404);

            if (!user.IsActive)
                return ServiceResult<string>.Fail("Hesabınız pasif durumdadır.", 403);

            string newPassword = GenerateRandomPassword(8);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                return ServiceResult<string>.Fail("Şifre sıfırlama hatası: " + result.Errors.First().Description, 500);
            }

            try
            {
                string mailBody = $@"
        <h3>Merhaba {user.FullName},</h3>
        <p>Hesabınız için şifre sıfırlama talebinde bulunuldu.</p>
        <p>Yeni Şifreniz: <strong style='font-size:18px; color:blue;'>{newPassword}</strong></p>
        <p>Lütfen giriş yaptıktan sonra şifrenizi değiştiriniz.</p>
        <br>
        <p>Sarfiyum Yönetimi</p>";

                await _emailService.SendEmailAsync(user.Email!, "Şifre Sıfırlama Bilgilendirmesi", mailBody);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Fail($"Şifre sıfırlandı ancak e-posta sunucusundaki bir hata nedeniyle size iletilemedi. (Hata: {ex.Message})", 500);
            }

            return ServiceResult<string>.Success("Yeni şifreniz e-posta adresinize gönderildi.", 200);
        }

        private string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$*";
            var random = new Random();
            var password = new StringBuilder();

            password.Append("A");
            password.Append("a");
            password.Append("1");
            password.Append("!");

            for (int i = 4; i < length; i++)
            {
                password.Append(validChars[random.Next(validChars.Length)]);
            }

            return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
        }

        private async Task LogLoginInternalAsync(string userId, string userName)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                string ipAddress = "0.0.0.0";
                string userAgent = "Unknown";

                if (httpContext != null)
                {
                    var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedHeader))
                    {
                        ipAddress = forwardedHeader.Split(',')[0].Trim();
                    }
                    else
                    {
                        ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
                    }

                    userAgent = httpContext.Request.Headers["User-Agent"].ToString();
                }

                var log = new UserLoginHistory
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    LoginDate = DateTime.UtcNow,
                    IsSuccess = true
                };

                await _loginHistoryRepo.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Loglama hatası akışı bozmamalı
            }
        }
    }
}