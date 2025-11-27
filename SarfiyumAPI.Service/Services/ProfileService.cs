using Microsoft.AspNetCore.Identity;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;
using System.Threading.Tasks;

namespace SarfiyumAPI.Service.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IRepository<Tenant> _tenantRepository;

        public ProfileService(UserManager<AppUser> userManager, IRepository<Tenant> tenantRepository)
        {
            _userManager = userManager;
            _tenantRepository = tenantRepository;
        }

        public async Task<ServiceResult<UserProfileDto>> GetProfileInfoAsync(string userId)
        {
            // 1. Kullanıcıyı Bul
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult<UserProfileDto>.Fail("Kullanıcı bulunamadı.", 404);

            // 2. Rolleri Çek
            var roles = await _userManager.GetRolesAsync(user);

            // 3. Temel DTO'yu Oluştur
            var profile = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                IsUserActive = user.IsActive,
                Roles = roles,
                TenantId = user.TenantId
            };

            // 4. Eğer Tenant varsa detayları çek ve doldur
            if (!string.IsNullOrEmpty(user.TenantId))
            {
                var tenant = await _tenantRepository.GetByIdAsync(user.TenantId);

                if (tenant != null)
                {
                    // Firma Temel Bilgileri
                    profile.TenantName = tenant.Name;
                    profile.TenantTaxNumber = tenant.TaxNumber;
                    profile.TenantPhoneNumber = tenant.PhoneNumber;
                    profile.TenantWebsite = tenant.Website;
                    profile.TenantLogoUrl = tenant.LogoUrl;
                    profile.IsTenantActive = tenant.IsActive;
                    profile.TenantCreatedAt = tenant.CreatedAt;

                    // Firma Adres Bilgileri (Address Value Object'ten geliyor)
                    profile.TenantCity = tenant.Address.City;
                    profile.TenantDistrict = tenant.Address.District;
                    profile.TenantCountry = tenant.Address.Country;
                    profile.TenantZipCode = tenant.Address.ZipCode;
                    profile.TenantFullAddress = tenant.Address.FullAddress;
                }
            }

            return ServiceResult<UserProfileDto>.Success(profile);
        }
    }
}