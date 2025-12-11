using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;
        private readonly IRepository<TenantProductCategory> _categoryRepository;
        private readonly IRepository<TenantProduct> _productRepository;
        public TenantService(IRepository<Tenant> tenantRepository, IDataContext context, IWebHostEnvironment env, IMemoryCache cache, IRepository<TenantProductCategory> categoryRepository,
            IRepository<TenantProduct> productRepository)
        {
            _tenantRepository = tenantRepository;
            _context = context;
            _env = env;
            _cache = cache;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
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
        // 3. OLUŞTUR (GÜNCELLENEN METOT)
        public async Task<ServiceResult<string>> CreateAsync(TenantDto model)
        {
            // A. Tenant Oluştur
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
                    ZipCode = model.ZipCode,
                    FullAddress = model.FullAddress,
                    Country = model.Country ?? "Türkiye"
                }
            };

            await _tenantRepository.AddAsync(tenant);
            await _context.SaveChangesAsync(); // ID oluşsun diye save ediyoruz.

            // ---------------------------------------------------------
            // B. KATEGORİLERİ OLUŞTUR (SEEDING)
            // ---------------------------------------------------------
            var catMaden = new TenantProductCategory { TenantId = tenant.Id, Name = "Maden", OrderIndex = 1 };
            var catDoviz = new TenantProductCategory { TenantId = tenant.Id, Name = "Döviz", OrderIndex = 2 };
            var catBilezik = new TenantProductCategory { TenantId = tenant.Id, Name = "Bilezik", OrderIndex = 3 }; // Boş gelecek
            var catKredi = new TenantProductCategory { TenantId = tenant.Id, Name = "Kredi Kartı", OrderIndex = 4 }; // Boş gelecek

            await _categoryRepository.AddRangeAsync(new[] { catMaden, catDoviz, catBilezik, catKredi });
            await _context.SaveChangesAsync(); // Kategori ID'leri oluşsun diye save ediyoruz.

            // ---------------------------------------------------------
            // C. VARSAYILAN ÜRÜNLERİ EKLE (Sadece Ham Veriler)
            // ---------------------------------------------------------
            var defaultProducts = new List<TenantProduct>();
            int order = 0;

            // --- DÖVİZ ---
            defaultProducts.Add(CreateProduct(tenant.Id, catDoviz.Id, "Dolar/TRY", "USDTRY", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catDoviz.Id, "Euro/TRY", "EURTRY", ++order));

            // --- MADEN ---
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Has Altın", "ALTIN", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Gümüş", "GUMTRY", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Külçe Altın", "KULCEALTIN", ++order));

            // Gramlar
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "22 Ayar Gram", "AYAR22", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "14 Ayar Gram", "AYAR14", ++order));

            // Ziynetler (Yeni)
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Yeni Çeyrek", "CEYREK_YENI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Yeni Yarım", "YARIM_YENI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Yeni Tam", "TEK_YENI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Yeni Ata", "ATA_YENI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Yeni Ata 5'li", "ATA5_YENI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Yeni Gremese", "GREMESE_YENI", ++order));

            // Ziynetler (Eski)
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Eski Çeyrek", "CEYREK_ESKI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Eski Yarım", "YARIM_ESKI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Eski Tam", "TEK_ESKI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Eski Ata", "ATA_ESKI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Eski Ata 5'li", "ATA5_ESKI", ++order));
            defaultProducts.Add(CreateProduct(tenant.Id, catMaden.Id, "Eski Gremese", "GREMESE_ESKI", ++order));

            // Bilezik ve Kredi Kartı kategorileri boş kalacak (User kendi ekleyecek)

            await _productRepository.AddRangeAsync(defaultProducts);
            await _context.SaveChangesAsync();

            // Cache Temizle
            ClearTenantCache();

            return ServiceResult<string>.Success(tenant.Id, 201);
        }

        private TenantProduct CreateProduct(string tenantId, string catId, string name, string sourceKey, int order)
        {
            return new TenantProduct
            {
                TenantId = tenantId,
                TenantProductCategoryId = catId,
                Name = name,
                SourceKey = sourceKey,
                BuyMultiplier = 1.0m, // Ham veri olduğu için çarpan 1
                SellMultiplier = 1.0m,
                AddonAmount = 0m,
                OrderIndex = order,
                IsActive = true
            };
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
            ClearTenantCache();
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
            ClearTenantCache();
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
            ClearTenantCache();
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


        public async Task<List<string>> GetActiveTenantIdsAsync()
        {
            string cacheKey = "Active_Tenant_List_Cached";

            // Cache kontrolü
            if (!_cache.TryGetValue(cacheKey, out List<string> tenantIds))
            {
                // DB'den çek
                // Not: Tenant.Id tipin int ise ToString() ile dönüştürüyoruz çünkü SignalR group string ister.
                tenantIds = await _tenantRepository
                    .Where(t => t.IsActive && !t.IsDeleted)
                    .Select(t => t.Id.ToString())
                    .ToListAsync();

                // Cache Ayarları (Örn: 10 dakika boyunca DB'ye gitme)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(cacheKey, tenantIds, cacheOptions);
            }

            return tenantIds;
        }

        public void ClearTenantCache()
        {
            _cache.Remove("Active_Tenant_List_Cached");
        }


    }
}