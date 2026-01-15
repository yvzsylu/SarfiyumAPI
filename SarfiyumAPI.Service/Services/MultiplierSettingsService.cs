using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class MultiplierSettingsService : IMultiplierSettingsService
    {
        // Eski Repositoryler silindi. Yerine TenantProduct geldi.
        private readonly IRepository<TenantProduct> _productRepository;
        private readonly IRepository<TenantProductCategory> _categoryRepository;
        private readonly IRepository<SystemCatalog> _catalogRepo;

        private readonly IDataContext _context;
        private readonly IMemoryCache _cache;

        public MultiplierSettingsService(
            IRepository<TenantProduct> productRepository,
            IDataContext context,
            IMemoryCache cache,
            IRepository<TenantProductCategory> categoryRepository,
            IRepository<SystemCatalog> catalogRepo)
        {
            _productRepository = productRepository;
            _context = context;
            _cache = cache;
            _categoryRepository = categoryRepository;
            _catalogRepo = catalogRepo;
        }

        // 1. WORKER SERVICE İÇİN CACHE'Lİ LİSTE GETİRME
        // (GoldPriceHostedService bunu kullanacak)
        public async Task<List<TenantProduct>> GetProductsAsync(string tenantId)
        {
            string cacheKey = $"TenantProducts_{tenantId}";

            // 1. Cache Kontrolü
            if (!_cache.TryGetValue(cacheKey, out List<TenantProduct> products))
            {
                // 2. Cache yoksa DB'den çek
                // Kategori bilgisiyle beraber çekiyoruz (Sıralama için önemli)
                products = await _productRepository
                    .Where(x => x.TenantId == tenantId && x.IsActive) // Sadece aktifler
                    .Include(x => x.TenantProductCategory)
                    .OrderBy(x => x.TenantProductCategory.OrderIndex) // Önce kategori sırası
                    .ThenBy(x => x.OrderIndex)                        // Sonra ürün sırası
                    .ToListAsync();

                // 3. Cache'e at (1 Saat)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(1))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(cacheKey, products, cacheOptions);
            }

            return products;
        }



        public async Task<ServiceResult<List<SystemCatalogDto>>> GetAvailableCatalogItemsAsync()
        {
            var catalogItems = await _catalogRepo.GetAllAsync();

            var dtoList = catalogItems.Select(item => new SystemCatalogDto
            {
                SourceKey = item.SourceKey,
                Name = item.Name
            })
            .OrderBy(x => x.Name) // Alfabetik sıra
            .ToList();

            return ServiceResult<List<SystemCatalogDto>>.Success(dtoList);
        }



        // 2. UI (ANGULAR) İÇİN AYARLARI GETİRME
        // (Tenant paneline girdiği zaman göreceği liste)
        public async Task<ServiceResult<List<TenantSettingResponseDto>>> GetSettingsForUIAsync(string tenantId)
        {
            // Burada cache kullanmaya gerek yok, admin paneli anlık veri görsün.
            // Ayrıca pasif olanları da görebilmeli ki tekrar açabilsin.

            var products = await _productRepository
                .Where(x => x.TenantId == tenantId) // IsActive filtresi YOK, hepsini görsün
                .Include(x => x.TenantProductCategory)
                .OrderBy(x => x.TenantProductCategory.OrderIndex)
                .ThenBy(x => x.OrderIndex)
                .ToListAsync();

            // Entity -> DTO Dönüşümü
            var responseList = products.Select(p => new TenantSettingResponseDto
            {
                Id = p.Id,                 // Artık Product ID önemli
                Name = p.Name,             // "22 Ayar Ajda"
                SourceKey = p.SourceKey,   // "ALTIN"
                CategoryName = p.TenantProductCategory?.Name ?? "Diğer",
                CategoryId = p.TenantProductCategoryId,
                BuyMultiplier = p.BuyMultiplier,
                SellMultiplier = p.SellMultiplier,
                AddonAmount = p.AddonAmount,
                IsActive = p.IsActive,
                OrderIndex = p.OrderIndex,
                ShowOnMobile = p.ShowOnMobile,
                ShowOnWeb = p.ShowOnWeb
                

            }).ToList();

            return ServiceResult<List<TenantSettingResponseDto>>.Success(responseList);
        }

        // 3. UI: KATEGORİ LİSTESİ (Dropdown İçin)
        public async Task<ServiceResult<List<TenantProductCategoryDto>>> GetCategoriesAsync(string tenantId)
        {
            var categories = await _categoryRepository
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            var dtos = categories.Select(c => new TenantProductCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                OrderIndex = c.OrderIndex
            }).ToList();

            return ServiceResult<List<TenantProductCategoryDto>>.Success(dtos);
        }

        // 4. UI: YENİ ÜRÜN EKLEME
        public async Task<ServiceResult<string>> CreateProductAsync(string tenantId, CreateTenantProductDto model)
        {
            // Kategori kontrolü (Tenant'a ait mi?)
            var category = await _categoryRepository
                .Where(c => c.Id == model.CategoryId && c.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (category == null)
                return ServiceResult<string>.Fail("Geçersiz kategori.", 400);

            var newProduct = new TenantProduct
            {
                TenantId = tenantId,
                TenantProductCategoryId = model.CategoryId,
                Name = model.Name,
                SourceKey = model.SourceKey, // Örn: "ALTIN"

                BuyMultiplier = model.BuyMultiplier,
                SellMultiplier = model.SellMultiplier,
                AddonAmount = model.AddonAmount,

                OrderIndex = model.OrderIndex,
                IsActive = true,
                ShowOnMobile = model.ShowOnMobile,
                ShowOnWeb = model.ShowOnWeb
            };

            await _productRepository.AddAsync(newProduct);
            await _context.SaveChangesAsync();

            ClearCache(tenantId); // Worker yeni ürünü görsün

            return ServiceResult<string>.Success(newProduct.Id, 201);
        }


        // 5. UI: TOPLU GÜNCELLEME (İsimler ve Çarpanlar)
        // Metod ismini 'UpdateProductsAsync' yaptım ve DTO'yu 'UpdateTenantProductDto' olarak güncelledim.
        public async Task<ServiceResult<string>> UpdateProductsAsync(string tenantId, List<UpdateTenantProductDto> updates)
        {
            var existingProducts = await _productRepository
                .Where(t => t.TenantId == tenantId)
                .ToListAsync();

            bool dataChanged = false;

            foreach (var updateItem in updates)
            {
                var product = existingProducts.FirstOrDefault(x => x.Id == updateItem.Id);
                if (product != null)
                {
                    // Değerleri güncelle
                    product.Name = updateItem.Name; // İsim değişikliği
                    product.BuyMultiplier = updateItem.BuyMultiplier;
                    product.SellMultiplier = updateItem.SellMultiplier;
                    product.AddonAmount = updateItem.AddonAmount;
                    product.IsActive = updateItem.IsActive;
                    product.TenantProductCategoryId = updateItem.CategoryId;    
                    product.ShowOnWeb = updateItem.ShowOnWeb;
                    product.ShowOnMobile = updateItem.ShowOnMobile;
                    _productRepository.Update(product);
                    dataChanged = true;
                }
            }

            if (dataChanged)
            {
                await _context.SaveChangesAsync();
                ClearCache(tenantId);
            }

            return ServiceResult<string>.Success("Ürünler güncellendi.", 200);
        }


        public async Task<ServiceResult<string>> ToggleWebStatusAsync(string tenantId, string productId)
        {
            var product = await _productRepository
                .Where(x => x.Id == productId && x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (product == null)
                return ServiceResult<string>.Fail("Ürün bulunamadı.", 404);

            // Mevcut durumun tersini al
            product.ShowOnWeb = !product.ShowOnWeb;

            _productRepository.Update(product);
            await _context.SaveChangesAsync();

            ClearCache(tenantId);

            return ServiceResult<string>.Success(product.ShowOnWeb ? "Ürün Web'de aktif edildi." : "Ürün Web'den gizlendi.", 200);
        }




        // 2. MOBİL GÖRÜNÜRLÜĞÜNÜ DEĞİŞTİR (Toggle)
        public async Task<ServiceResult<string>> ToggleMobileStatusAsync(string tenantId, string productId)
        {
            var product = await _productRepository
                .Where(x => x.Id == productId && x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (product == null)
                return ServiceResult<string>.Fail("Ürün bulunamadı.", 404);

            // Mevcut durumun tersini al
            product.ShowOnMobile = !product.ShowOnMobile;

            _productRepository.Update(product);
            await _context.SaveChangesAsync();

            ClearCache(tenantId);

            return ServiceResult<string>.Success(product.ShowOnMobile ? "Ürün Mobilde aktif edildi." : "Ürün Mobilden gizlendi.", 200);
        }


        // 6. UI: TEKİL DURUM GÜNCELLEME (Aktif/Pasif)
        public async Task<ServiceResult<string>> UpdateProductStatusAsync(string tenantId, string productId, bool isActive)
        {
            var product = await _productRepository
                .Where(x => x.Id == productId && x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (product == null)
                return ServiceResult<string>.Fail("Ürün bulunamadı.", 404);

            product.IsActive = isActive;
            _productRepository.Update(product);
            await _context.SaveChangesAsync();

            ClearCache(tenantId);

            return ServiceResult<string>.Success(isActive ? "Ürün aktife alındı." : "Ürün pasife çekildi.", 200);
        }
        // 7. UI: ÜRÜN SİLME
        public async Task<ServiceResult<string>> DeleteProductAsync(string tenantId, string productId)
        {
            var product = await _productRepository
                .Where(p => p.Id == productId && p.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (product == null)
                return ServiceResult<string>.Fail("Ürün bulunamadı.", 404);

            _productRepository.Remove(product);
            await _context.SaveChangesAsync();

            ClearCache(tenantId);

            return ServiceResult<string>.Success("Ürün silindi.", 200);
        }
        // --- CACHE TEMİZLEME YARDIMCISI ---
        public void ClearCache(string tenantId)
        {
            _cache.Remove($"TenantProducts_{tenantId}");
        }

        // YENİ METODU EKLE: UpdateProductOrderAsync (En alta ekleyebilirsin)
        public async Task<ServiceResult<string>> UpdateProductOrderAsync(string tenantId, List<ProductOrderRequestDto> orderList)
        {
            // 1. Tenant'ın ürünlerini çek
            var allProducts = await _productRepository
                .Where(x => x.TenantId == tenantId)
                .ToListAsync();

            bool dataChanged = false;

            // 2. Gelen listeyi eşleştir ve güncelle
            foreach (var orderItem in orderList)
            {
                var product = allProducts.FirstOrDefault(p => p.Id == orderItem.ProductId);

                // Eğer ürün varsa ve sırası değişmişse güncelle
                if (product != null && product.OrderIndex != orderItem.NewIndex)
                {
                    product.OrderIndex = orderItem.NewIndex;
                    _productRepository.Update(product);
                    dataChanged = true;
                }
            }

            // 3. Değişiklik varsa kaydet ve Cache temizle
            if (dataChanged)
            {
                await _context.SaveChangesAsync();
                ClearCache(tenantId); // Kritik: HostedService yeni sırayı görsün
            }

            return ServiceResult<string>.Success("Sıralama güncellendi.", 200);
        }

    }
}