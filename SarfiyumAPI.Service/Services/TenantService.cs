using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.ValueObjects;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class TenantService : ITenantService
    {
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IDataContext _context;

        public TenantService(IRepository<Tenant> tenantRepository, IDataContext context)
        {
            _tenantRepository = tenantRepository;
            _context = context;
        }

        // 1. TÜMÜNÜ GETİR
        public async Task<ServiceResult<List<TenantDto>>> GetAllAsync()
        {
            var tenants = await _tenantRepository.Where(x => !x.IsDeleted).ToListAsync();

            var dtos = tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                TaxNumber = t.TaxNumber,
                PhoneNumber = t.PhoneNumber,
                Website = t.Website,
                LogoUrl = t.LogoUrl,
                IsActive = t.IsActive,
                // Address Mapping
                City = t.Address.City,
                District = t.Address.District,
                // Street YOK
                ZipCode = t.Address.ZipCode,
                FullAddress = t.Address.FullAddress,
                Country = t.Address.Country
            }).ToList();

            return ServiceResult<List<TenantDto>>.Success(dtos);
        }

        // 2. ID İLE GETİR
        public async Task<ServiceResult<TenantDto>> GetByIdAsync(string id)
        {
            var t = await _tenantRepository.GetByIdAsync(id);
            if (t == null || t.IsDeleted) return ServiceResult<TenantDto>.Fail("Tenant not found", 404);

            var dto = new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                TaxNumber = t.TaxNumber,
                PhoneNumber = t.PhoneNumber,
                Website = t.Website,
                LogoUrl = t.LogoUrl,
                IsActive = t.IsActive,
                City = t.Address.City,
                District = t.Address.District,
                // Street YOK
                ZipCode = t.Address.ZipCode,
                FullAddress = t.Address.FullAddress,
                Country = t.Address.Country
            };

            return ServiceResult<TenantDto>.Success(dto);
        }

        // 3. OLUŞTUR
        public async Task<ServiceResult<string>> CreateAsync(TenantDto model)
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name,
                TaxNumber = model.TaxNumber,
                PhoneNumber = model.PhoneNumber,
                Website = model.Website,
                LogoUrl = model.LogoUrl,
                IsActive = true,
                Address = new Address
                {
                    City = model.City,
                    District = model.District,
                    // Street YOK
                    ZipCode = model.ZipCode,
                    FullAddress = model.FullAddress,
                    Country = model.Country ?? "Türkiye"
                }
            };

            await _tenantRepository.AddAsync(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success(tenant.Id, 201);
        }

        // 4. GÜNCELLE
        public async Task<ServiceResult<string>> UpdateAsync(TenantDto model)
        {
            var tenant = await _tenantRepository.GetByIdAsync(model.Id!);
            if (tenant == null || tenant.IsDeleted) return ServiceResult<string>.Fail("Tenant not found", 404);

            tenant.Name = model.Name;
            tenant.TaxNumber = model.TaxNumber;
            tenant.PhoneNumber = model.PhoneNumber;
            tenant.Website = model.Website;
            tenant.LogoUrl = model.LogoUrl;
            tenant.Address.City = model.City;
            tenant.Address.District = model.District;
            // Street YOK
            tenant.Address.ZipCode = model.ZipCode;
            tenant.Address.FullAddress = model.FullAddress;
            tenant.Address.Country = model.Country;

            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Tenant updated successfully", 200);
        }

        // 5. SİL (Soft Delete)
        public async Task<ServiceResult<string>> DeleteAsync(string id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null) return ServiceResult<string>.Fail("Tenant not found", 404);

            // MANUEL SOFT DELETE
            tenant.IsDeleted = true;
            tenant.RemovedAt = DateTime.UtcNow;

            // Street kontrolü veya dummy veri atamasına gerek kalmadı
            // Çünkü Street kolonu veritabanından silindi.

            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Tenant deleted successfully", 200);
        }

        public async Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null) return ServiceResult<string>.Fail("Tenant not found", 404);

            tenant.IsActive = isActive;
            // Diğer alanlara dokunmuyoruz, sadece IsActive değişiyor.
            // ModifiedAt otomatik güncellenecek (EntityBase).

            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Status updated successfully", 200);
        }
    }
}