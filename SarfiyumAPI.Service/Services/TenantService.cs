using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IWebHostEnvironment _env;
        public TenantService(IRepository<Tenant> tenantRepository, IDataContext context, IWebHostEnvironment env)
        {
            _tenantRepository = tenantRepository;
            _context = context;
            _env = env; 
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

        public async Task<ServiceResult<string>> UploadLogoAsync(string tenantId, IFormFile file)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null) return ServiceResult<string>.Fail("Firma bulunamadı.", 404);

            if (file == null || file.Length == 0)
                return ServiceResult<string>.Fail("Dosya seçilmedi.", 400);

            // --- HATA ÇÖZÜMÜ BURADA ---
            // WebRootPath null ise (wwwroot klasörü yoksa), ContentRootPath üzerinden oluştur.
            string webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

            string uploadsFolder = Path.Combine(webRootPath, "uploads", "logos");
            // --------------------------

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // 2. Benzersiz Dosya Adı Oluştur
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 3. Dosyayı Kaydet
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 4. DB'yi Güncelle
            // URL formatı: /uploads/logos/dosyaadi.png
            string logoUrl = $"/uploads/logos/{uniqueFileName}";

            tenant.LogoUrl = logoUrl;
            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success(logoUrl, 200);
        }
    }
}