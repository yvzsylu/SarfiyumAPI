using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;

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
            { "TEK_YENI", (40085m, 40924m) }, // Yeni Tam
            { "ATA_YENI", (41742m, 42076m) },
            { "ATA5_YENI", (207618m, 209920m) },
            { "GREMESE_YENI", (100057m, 101996m) },
            { "CEYREK_ESKI", (10006m, 10111m) },
            { "YARIM_ESKI", (20011m, 20192m) },
            { "TEK_ESKI", (40085m, 40484m) }, // Eski Tam
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
            _logger.LogInformation("🛠️ MOCK Veri Yayını Veritabanı Keylerine Göre Başladı (USDTRY, ALTIN, CEYREK_YENI vb.)...");

            while (!stoppingToken.IsCancellationRequested)
            {
                await BroadcastToTenants();
                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task BroadcastToTenants()
        {
            using var scope = _serviceProvider.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ITenantSettingsService>();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

            var activeTenantIds = await tenantService.GetActiveTenantIdsAsync();
            if (activeTenantIds == null) return;

            foreach (var tenantId in activeTenantIds)
            {
                var myProducts = await settingsService.GetProductsAsync(tenantId);
                if (myProducts == null) continue;

                var preparedList = new List<PriceData>();

                foreach (var product in myProducts)
                {
                    // 🔥 Veritabanından gelen SourceKey (Örn: "USDTRY")
                    string key = product.SourceKey?.ToUpper().Trim() ?? "UNKNOWN";

                    if (!_priceCache.TryGetValue(key, out var sourceData))
                    {
                        if (_marketDefaults.TryGetValue(key, out var val))
                        {
                            sourceData = new PriceData { Symbol = key, Bid = val.Bid, Ask = val.Ask };
                        }
                        else
                        {
                            // Listedeki anahtarlardan biri değilse (Örn: Manuel eklenen bir ürün)
                            sourceData = new PriceData { Symbol = key, Bid = 1000m, Ask = 1010m };
                        }
                        _priceCache.TryAdd(key, sourceData);
                    }

                    // Fiyatları gerçekçi oynat
                    decimal vol = sourceData.Bid < 200 ? 0.005m : (sourceData.Bid > 10000 ? 2.5m : 0.6m);
                    decimal change = (decimal)(_random.NextDouble() * 2 - 1) * vol;
                    sourceData.Bid += change;
                    sourceData.Ask += change;
                    sourceData.Timestamp = DateTime.Now.ToString("O");

                    // Gerçek çarpan ve makas hesaplaması
                    decimal finalBid = (sourceData.Bid ?? 0) * product.BuyMultiplier - product.AddonAmount;
                    decimal finalAsk = (sourceData.Ask ?? 0) * product.SellMultiplier + product.AddonAmount;

                    preparedList.Add(new PriceData
                    {
                        Symbol = product.Name, // Angular'da görünecek isim (Örn: "Has Altın")
                        Category = product.TenantProductCategory?.Name ?? "Genel",
                        Description = product.Name,
                        Source = "MOCK-LIVE",
                        Timestamp = sourceData.Timestamp,
                        Bid = finalBid,
                        Ask = finalAsk,
                        OrderIndex = product.OrderIndex
                    });
                }

                if (preparedList.Any())
                {
                    await _hubContext.Clients.Group(tenantId).SendAsync("ReceivePrices", preparedList);
                }
            }
        }
    }
}