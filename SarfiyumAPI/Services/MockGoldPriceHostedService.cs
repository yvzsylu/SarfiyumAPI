using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;
using SarfiyumAPI.Core.Entities; // Entity referansı için
using System.Linq;

namespace SarfiyumAPI.Services
{
    public class MockGoldPriceHostedService : BackgroundService
    {
        private readonly IHubContext<GoldHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MockGoldPriceHostedService> _logger;
        private readonly ConcurrentDictionary<string, PriceData> _priceCache = new();
        private readonly Random _random = new();

        // 🔥 Veritabanındaki SourceKey kolonunla birebir eşlenmiş liste
        private readonly Dictionary<string, (decimal Bid, decimal Ask)> _marketDefaults = new()
        {
            { "USDTRY", (42.8754m, 42.9789m) },
            { "EURTRY", (50.4108m, 50.6348m) },
            { "ALTIN", (6253.55m, 6292.94m) },
            { "GUMTRY", (99.1625m, 104.2916m) },
            { "KULCEALTIN", (6222.28m, 6311.82m) },
            { "AYAR22", (5688.73m, 5958.29m) },
            { "AYAR14", (3441.99m, 4590.41m) },
            { "CEYREK_YENI", (10024m, 10281m) },
            { "YARIM_YENI", (20030m, 20556m) },
            { "TEK_YENI", (40085m, 40924m) },
            { "ATA_YENI", (41742m, 42076m) },
            { "ATA5_YENI", (207618m, 209920m) },
            { "GREMESE_YENI", (100057m, 101996m) },
            { "CEYREK_ESKI", (10006m, 10111m) },
            { "YARIM_ESKI", (20011m, 20192m) },
            { "TEK_ESKI", (40085m, 40484m) },
            { "ATA_ESKI", (41649m, 41994m) },
            { "ATA5_ESKI", (206993m, 208976m) },
            { "GREMESE_ESKI", (100057m, 101366m) }
        };

        public MockGoldPriceHostedService(IHubContext<GoldHub> hubContext, IServiceProvider serviceProvider, ILogger<MockGoldPriceHostedService> logger)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛠️ MOCK Veri Yayını (Visitor Override Destekli) Başladı...");

            while (!stoppingToken.IsCancellationRequested)
            {
                // 1. Önce Ham Verileri Güncelle (Her döngüde fiyatlar biraz oynasın)
                UpdateMockData();

                // 2. Sonra Yayınla
                await BroadcastToTenants();

                await Task.Delay(2000, stoppingToken);
            }
        }

        private void UpdateMockData()
        {
            foreach (var kvp in _marketDefaults)
            {
                string key = kvp.Key;
                var baseVal = kvp.Value;

                // Cache'de var mı? Varsa onu al, yoksa default'u al
                if (!_priceCache.TryGetValue(key, out var sourceData))
                {
                    sourceData = new PriceData { Symbol = key, Bid = baseVal.Bid, Ask = baseVal.Ask };
                }

                // Fiyatları gerçekçi oynat
                decimal vol = (sourceData.Bid ?? 0) < 200 ? 0.005m : ((sourceData.Bid ?? 0) > 10000 ? 2.5m : 0.6m);
                decimal change = (decimal)(_random.NextDouble() * 2 - 1) * vol;

                // Yeni değerleri set et
                sourceData.Bid = (sourceData.Bid ?? baseVal.Bid) + change;
                sourceData.Ask = (sourceData.Ask ?? baseVal.Ask) + change;
                sourceData.Timestamp = DateTime.Now.ToString("O");

                _priceCache.AddOrUpdate(key, sourceData, (k, old) => sourceData);
            }
        }

        private async Task BroadcastToTenants()
        {
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<IMultiplierSettingsService>();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
            var visitorService = scope.ServiceProvider.GetRequiredService<IVisitorSettingService>(); // 🔥 YENİ

            var activeTenantIds = await tenantService.GetActiveTenantIdsAsync();
            if (activeTenantIds == null) return;

            // Cache'deki son ham verileri sözlüğe çevir (Hızlı erişim için)
            var latestMarketData = _priceCache.ToDictionary(x => x.Key, x => x.Value);

            foreach (var tenantId in activeTenantIds)
            {
                var myProducts = await settingsService.GetProductsAsync(tenantId);
                if (myProducts == null || !myProducts.Any()) continue;

                // -----------------------------------------------------------
                // 1. STANDART YAYIN (GRUP)
                // -----------------------------------------------------------
                var standardList = CalculatePriceList(myProducts, latestMarketData, null);

                if (standardList.Any())
                {
                    // Mock serviste de grubu "_Standard" olarak güncelledik
                    await _hubContext.Clients.Group($"{tenantId}_Standard").SendAsync("ReceivePrices", standardList);
                }

                // -----------------------------------------------------------
                // 2. ÖZEL KULLANICILAR (KİŞİYE ÖZEL)
                // -----------------------------------------------------------
                var specialVisitors = await visitorService.GetOnlineVisitorsWithSettingsAsync(tenantId);

                _logger.LogInformation($"Tenant: {tenantId} -> Online Özel Ayarlı Kullanıcı Sayısı: {specialVisitors.Count}");

                foreach (var visitorSession in specialVisitors)
                {
                    var specialList = CalculatePriceList(myProducts, latestMarketData, visitorSession.Settings);

                    // Mock veriyi özel kullanıcıya gönder
                    await _hubContext.Clients.Group(visitorSession.VisitorId).SendAsync("ReceivePrices", specialList);
                }
            }
        }

        // 🔥 ORTAK HESAPLAMA METODU (Calculate logic buraya taşındı)
        private List<PriceData> CalculatePriceList(
            List<TenantProduct> products,
            Dictionary<string, PriceData> marketData,
            List<VisitorProductSetting>? specialSettings)
        {
            var list = new List<PriceData>();

            foreach (var product in products)
            {
                string key = product.SourceKey?.ToUpper().Trim() ?? "UNKNOWN";

                // Eğer market verisi yoksa (örn: manuel ürün), varsayılan bir değer uydur
                if (!marketData.TryGetValue(key, out var sourceData))
                {
                    sourceData = new PriceData { Symbol = key, Bid = 1000m, Ask = 1010m, Timestamp = DateTime.Now.ToString("O") };
                }

                decimal buyMult = product.BuyMultiplier;
                decimal sellMult = product.SellMultiplier;
                decimal addon = product.AddonAmount;

                // 🔥 EĞER ÖZEL AYAR VARSA DEĞERLERİ EZ (OVERRIDE)
                if (specialSettings != null)
                {
                    var setting = specialSettings.FirstOrDefault(s => s.TenantProductId == product.Id);
                    if (setting != null)
                    {
                        if (setting.SpecialBuyMultiplier.HasValue) buyMult = setting.SpecialBuyMultiplier.Value;
                        if (setting.SpecialSellMultiplier.HasValue) sellMult = setting.SpecialSellMultiplier.Value;
                        if (setting.SpecialAddonAmount.HasValue) addon = setting.SpecialAddonAmount.Value;
                    }
                }

                decimal finalBid = (sourceData.Bid ?? 0) * buyMult - addon;
                decimal finalAsk = (sourceData.Ask ?? 0) * sellMult + addon;

                list.Add(new PriceData
                {
                    Symbol = product.Name,
                    Category = product.TenantProductCategory?.Name ?? "Genel",
                    CategoryIndex = product.TenantProductCategory?.OrderIndex ?? 0,
                    Description = product.Name,
                    Source = "MOCK-LIVE",
                    Timestamp = sourceData.Timestamp,
                    Bid = finalBid,
                    Ask = finalAsk,
                    OrderIndex = product.OrderIndex
                });
            }
            return list;
        }
    }
}