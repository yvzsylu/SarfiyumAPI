using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;
using SocketIOClient;
using SocketIOClient.Transport;
using SarfiyumAPI.Core.Entities;
using System.Linq;

namespace SarfiyumAPI.Services
{
    public class GoldPriceHostedService : BackgroundService
    {
        private readonly IHubContext<GoldHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GoldPriceHostedService> _logger;
        private SocketIOClient.SocketIO _client;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, PriceData> _priceCache = new();

        public GoldPriceHostedService(
            IHubContext<GoldHub> hubContext,
            IServiceProvider serviceProvider,
            ILogger<GoldPriceHostedService> logger,
            IConfiguration configuration)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 GoldPriceHostedService Başlatılıyor...");

            string serverUrl = _configuration["SocketServer:Url"] ?? throw new Exception("Url Yok");
            string myKey = _configuration["SocketServer:Key"] ?? throw new Exception("Key Yok");

            _client = new SocketIOClient.SocketIO(serverUrl, new SocketIOOptions
            {
                Auth = new { socket_key = myKey },
                Query = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("socket_key", myKey) },
                Transport = TransportProtocol.Polling | TransportProtocol.WebSocket,
                EIO = SocketIO.Core.EngineIO.V4,
                Reconnection = true,
                ReconnectionDelay = 1000
            });

            _client.OnConnected += async (sender, e) => _logger.LogInformation($"✅ Socket.IO Bağlantısı Başarılı!");

            _client.On("price_changed", async response =>
            {
                try
                {
                    var incomingList = response.GetValue<List<PriceData>>();
                    if (incomingList != null && incomingList.Any())
                    {
                        foreach (var item in incomingList)
                        {
                            _priceCache.AddOrUpdate(item.Symbol, item, (key, oldValue) => item);
                        }
                        await BroadcastToTenants();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Veri işleme hatası: {ex.Message}");
                }
            });

            try { await _client.ConnectAsync(); }
            catch (Exception ex) { _logger.LogError($"Socket Hata: {ex.Message}"); }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            if (_client.Connected) await _client.DisconnectAsync();
        }

        private async Task BroadcastToTenants()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var settingsService = scope.ServiceProvider.GetRequiredService<IMultiplierSettingsService>();
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
                var visitorService = scope.ServiceProvider.GetRequiredService<IVisitorSettingService>(); // 🔥 YENİ

                var activeTenantIds = await tenantService.GetActiveTenantIdsAsync();
                if (activeTenantIds == null || !activeTenantIds.Any()) return;

                // Cache'teki son durumu al
                var latestMarketData = _priceCache.Values.ToDictionary(x => x.Symbol, x => x);

                foreach (var tenantId in activeTenantIds)
                {
                    var myProducts = await settingsService.GetProductsAsync(tenantId);
                    if (myProducts == null || !myProducts.Any()) continue;

                    // -----------------------------------------------------------
                    // 1. STANDART YAYIN (GRUP)
                    // -----------------------------------------------------------
                    // Herkesin gördüğü varsayılan listeyi hesapla
                    var standardList = CalculatePriceList(myProducts, latestMarketData, null);

                    if (standardList.Any())
                    {
                        // Sadece standart gruba gönder
                        await _hubContext.Clients.Group($"{tenantId}_Standard").SendAsync("ReceivePrices", standardList);
                    }

                    // -----------------------------------------------------------
                    // 2. ÖZEL KULLANICILAR (KİŞİYE ÖZEL)
                    // -----------------------------------------------------------
                    // Bu tenant'a bağlı olup şu an online olan ve özel ayarı olanları getir
                    var specialVisitors = await visitorService.GetOnlineVisitorsWithSettingsAsync(tenantId);

                    foreach (var visitorSession in specialVisitors)
                    {
                        // Bu kullanıcıya özel listeyi hesapla (VisitorProductSettings gönderiyoruz)
                        var specialList = CalculatePriceList(myProducts, latestMarketData, visitorSession.Settings);

                        // Doğrudan o kullanıcıya gönder
                        await _hubContext.Clients.Group(visitorSession.VisitorId).SendAsync("ReceivePrices", specialList);
                    }
                }
            }
        }

        // 🔥 TEK BİR HESAPLAMA METODU (Hem Standart Hem Özel İçin)
        private List<PriceData> CalculatePriceList(
            List<TenantProduct> products,
            Dictionary<string, PriceData> marketData,
            List<VisitorProductSetting>? specialSettings)
        {
            var list = new List<PriceData>();

            foreach (var product in products)
            {
                if (marketData.TryGetValue(product.SourceKey, out var sourceData))
                {
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

                    // Hesaplama
                    decimal finalBid = 0;
                    decimal finalAsk = 0;

                    if (sourceData.Bid.HasValue)
                        finalBid = (sourceData.Bid.Value * buyMult) - addon;

                    if (sourceData.Ask.HasValue)
                        finalAsk = (sourceData.Ask.Value * sellMult) + addon;

                    list.Add(new PriceData
                    {
                        Symbol = product.Name,
                        Category = product.TenantProductCategory?.Name ?? "Genel",
                        CategoryIndex = product.TenantProductCategory?.OrderIndex ?? 0,
                        Description = product.Name,
                        Source = "Sarfiyum",
                        Timestamp = sourceData.Timestamp,
                        Bid = finalBid > 0 ? finalBid : null,
                        Ask = finalAsk > 0 ? finalAsk : null,
                        OrderIndex = product.OrderIndex
                    });
                }
            }
            return list;
        }
    }
}