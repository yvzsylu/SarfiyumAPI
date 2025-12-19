using Microsoft.AspNetCore.Http;
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
        private readonly IEmailService _emailService; // Mail servisini inject ediyoruz

        // 2. DEĞİŞİKLİK: Tenant oluşturmak için Repository ekliyoruz
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<UserLoginHistory> _loginHistoryRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IDataContext context,
            IRepository<Tenant> tenantRepository,
            IEmailService emailService,
            IRepository<UserLoginHistory> loginHistoryRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
            _tenantRepository = tenantRepository;
            _emailService = emailService;
            _loginHistoryRepo = loginHistoryRepo;
            _httpContextAccessor = httpContextAccessor;
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

                    // B. Abonelik Varlığı Kontrolü (Hiç paket almamışsa)
                    if (!tenant.SubscriptionEndDate.HasValue)
                    {
                        return ServiceResult<string>.Fail("Firmanıza tanımlı herhangi bir abonelik bulunamadı. Giriş yapabilmek için lütfen bir paket satın alınız.", 403);
                    }

                    // C. Abonelik Süresi Kontrolü (Süresi dolmuşsa)
                    if (tenant.SubscriptionEndDate.Value < DateTime.UtcNow)
                    {
                        string bitisTarihi = tenant.SubscriptionEndDate.Value.ToString("dd.MM.yyyy");
                        return ServiceResult<string>.Fail($"Abonelik süreniz {bitisTarihi} tarihinde dolmuştur. Sisteme giriş yapabilmek için lütfen aboneliğinizi yenileyiniz.", 403);
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
                    authClaims.Add(new Claim("tenantId", user.TenantId));

                    // Tenant detaylarını Repository'den çekiyoruz
                    var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);

                    if (tenant != null)
                    {
                        // Firma Adı
                        authClaims.Add(new Claim("tenantName", tenant.Name));

                        // Firma Logosu (Eğer varsa)
                        if (!string.IsNullOrEmpty(tenant.LogoUrl))
                        {
                            authClaims.Add(new Claim("tenantLogoUrl", tenant.LogoUrl));
                        }
                        // 🔥 TOKEN'A ABONELİK BİTİŞ TARİHİNİ EKLEME
                        // Frontend'de "Aboneliğinizin bitmesine 3 gün kaldı" gibi uyarılar göstermek için.
                        if (tenant.SubscriptionEndDate.HasValue)
                        {
                            authClaims.Add(new Claim("subscriptionEndDate", tenant.SubscriptionEndDate.Value.ToString("O"))); // ISO Format
                        }
                    }
                }

                // 3. Rolleri Ekle
                authClaims.AddRange(userRoles.Select(role => new Claim("roles", role)));

                var token = GetToken(authClaims);


                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                // --- YENİ: Giriş Kayıt Loglaması ---
                await LogLoginInternalAsync(user.Id, user.UserName);


                return ServiceResult<string>.Success(tokenString, 200);
            }


            return ServiceResult<string>.Fail("Invalid username or password!", 401);
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

        // YENİ: Şifre Sıfırlama Mantığı
        public async Task<ServiceResult<string>> ResetPasswordAsync(string email)
        {
            // 1. Kullanıcıyı Bul
            var user = await _userManager.FindByEmailAsync(email);

            // GÜVENLİK İYİLEŞTİRMESİ (User Enumeration Prevention):
            // Kötü niyetli kişiler rastgele mailler deneyerek "Kullanıcı bulunamadı" hatası alıp
            // sistemde kimlerin kayıtlı olduğunu öğrenemesin diye, kullanıcı yoksa bile
            // "Varsa mail gönderildi" diyip süreci sonlandırabiliriz.
            // ANCAK: Geliştirme aşamasında olduğun için şimdilik net hata dönmeye devam edebilirsin.
            // Canlıya geçerken burayı "Success" dönüp işlemi kesmek daha güvenlidir.
            if (user == null)
                return ServiceResult<string>.Fail("Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.", 404);

            // 2. Kullanıcı Aktif mi?
            if (!user.IsActive)
                return ServiceResult<string>.Fail("Hesabınız pasif durumdadır.", 403);

            // 3. 8 Haneli Güvenli Şifre Üret
            string newPassword = GenerateRandomPassword(8);

            // 4. Şifreyi Sıfırla
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                return ServiceResult<string>.Fail("Şifre sıfırlama hatası: " + result.Errors.First().Description, 500);
            }

            // 5. Mail Gönder
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
                // KRİTİK GÜVENLİK DÜZELTMESİ:
                // Şifre veritabanında değişti ama kullanıcıya mail gitmedi.
                // ASLA yeni şifreyi burada gösterme!
                // Kullanıcı giriş yapamayacak ama tekrar "Şifremi Unuttum" diyerek tekrar deneyebilir.
                // Bu durum, hesabın çalınmasından daha iyidir.

                // Loglama yapmalısın (Örn: Serilog ile)
                // _logger.LogError(ex, $"Mail gönderilemedi: {email}");

                return ServiceResult<string>.Fail($"Şifre sıfırlandı ancak e-posta sunucusundaki bir hata nedeniyle size iletilemedi. Lütfen daha sonra tekrar deneyiniz veya yönetici ile iletişime geçiniz. (Hata: {ex.Message})", 500);
            }

            return ServiceResult<string>.Success("Yeni şifreniz e-posta adresinize gönderildi.", 200);
        }

        // YARDIMCI METOT: Rastgele Şifre Oluşturucu
        private string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$*";
            var random = new Random();
            var password = new StringBuilder();

            // Identity kurallarına (En az 1 büyük, 1 küçük, 1 sayı, 1 özel karakter) uyum sağlamak için garanti karakterler
            password.Append("A"); // Büyük harf
            password.Append("a"); // Küçük harf
            password.Append("1"); // Rakam
            password.Append("!"); // Özel karakter

            // Kalan karakterleri rastgele doldur
            for (int i = 4; i < length; i++)
            {
                password.Append(validChars[random.Next(validChars.Length)]);
            }

            // Karakterleri karıştır (Shuffle) ki hep Aa1! ile başlamasın
            return new string(password.ToString().OrderBy(x => random.Next()).ToArray());
        }

        // --- YENİ EKLENEN METOD: LOGIN KAYDI ---
        // Bu metodu private yapıp içeride çağırıyoruz. Dışarı açmaya gerek yok.
        private async Task LogLoginInternalAsync(string userId, string userName)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                string ipAddress = "0.0.0.0";
                string userAgent = "Unknown";

                if (httpContext != null)
                {
                    // IP Alma Mantığı (Proxy destekli)
                    var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedHeader))
                    {
                        ipAddress = forwardedHeader.Split(',')[0].Trim();
                    }
                    else
                    {
                        ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
                    }

                    // User Agent
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
                // Loglama hatası login sürecini patlatmamalı
                // Burada Console.WriteLine veya ILogger kullanılabilir
            }
        }

    }
}