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
            var tenants = await _tenantRepository.GetAllAsync();

            var dtos = tenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                TaxNumber = t.TaxNumber,
                PhoneNumber = t.PhoneNumber,

                // Address Mapping (Entity -> DTO)
                City = t.Address.City,
                District = t.Address.District,
                Street = t.Address.Street,
                FullAddress = t.Address.FullAddress,
                ZipCode = t.Address.ZipCode
            }).ToList();

            return ServiceResult<List<TenantDto>>.Success(dtos);
        }

        // 2. ID İLE GETİR
        public async Task<ServiceResult<TenantDto>> GetByIdAsync(string id)
        {
            var t = await _tenantRepository.GetByIdAsync(id);
            if (t == null) return ServiceResult<TenantDto>.Fail("Tenant not found", 404);

            var dto = new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                TaxNumber = t.TaxNumber,
                PhoneNumber = t.PhoneNumber,

                City = t.Address.City,
                District = t.Address.District,
                Street = t.Address.Street,
                FullAddress = t.Address.FullAddress,
                ZipCode = t.Address.ZipCode
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

                // DTO -> Address Value Object Mapping
                Address = new Address
                {
                    City = model.City,
                    District = model.District,
                    Street = model.Street,
                    FullAddress = model.FullAddress,
                    ZipCode = model.ZipCode,
                    Country = "Türkiye" // Varsayılan
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
            if (tenant == null) return ServiceResult<string>.Fail("Tenant not found", 404);

            tenant.Name = model.Name;
            tenant.TaxNumber = model.TaxNumber;
            tenant.PhoneNumber = model.PhoneNumber;

            // Adres Güncelleme
            tenant.Address.City = model.City;
            tenant.Address.District = model.District;
            tenant.Address.Street = model.Street;
            tenant.Address.FullAddress = model.FullAddress;
            tenant.Address.ZipCode = model.ZipCode;

            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Tenant updated successfully", 200);
        }

        // 5. SİL
        public async Task<ServiceResult<string>> DeleteAsync(string id)
        {
            var tenant = await _tenantRepository.GetByIdAsync(id);
            if (tenant == null) return ServiceResult<string>.Fail("Tenant not found", 404);

            // Soft Delete işlemi Repository/Context tarafında otomatik halledilir (IsDeleted = true)
            _tenantRepository.Remove(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Tenant deleted successfully", 200);
        }
    }
}