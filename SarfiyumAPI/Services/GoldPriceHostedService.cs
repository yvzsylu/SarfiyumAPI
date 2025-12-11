using System.Collections.Concurrent; // Buna ihtiyacımız var
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;
using SocketIOClient;
using SocketIOClient.Transport;

namespace SarfiyumAPI.Services
{
    public class GoldPriceHostedService : BackgroundService
    {
        private readonly IHubContext<GoldHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GoldPriceHostedService> _logger;
        private SocketIOClient.SocketIO _client;

        // 🔥 YENİ: Son fiyatları hafızada tutmak için Thread-Safe Sözlük
        private readonly ConcurrentDictionary<string, PriceData> _priceCache = new();

        public GoldPriceHostedService(
            IHubContext<GoldHub> hubContext,
            IServiceProvider serviceProvider,
            ILogger<GoldPriceHostedService> logger)
        {
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 GoldPriceHostedService Başlatılıyor...");

            const string serverUrl = "https://sse.datshop.com.tr";
            const string myKey = "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";

            _client = new SocketIOClient.SocketIO(serverUrl, new SocketIOOptions
            {
                Auth = new { socket_key = myKey },
                Query = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("socket_key", myKey) },
                Transport = TransportProtocol.Polling | TransportProtocol.WebSocket,
                EIO = SocketIO.Core.EngineIO.V4,
                Reconnection = true,
                ReconnectionDelay = 1000
            });

            _client.OnConnected += async (sender, e) => _logger.LogInformation($"✅ Socket.IO Bağlantısı Başarılı! ({_client.Id})");

            // 🔥 VERİ GELDİĞİNDE YAPILACAKLAR DEĞİŞTİ
            _client.On("price_changed", async response =>
            {
                try
                {
                    var incomingList = response.GetValue<List<PriceData>>();
                    if (incomingList != null && incomingList.Any())
                    {
                        // 1. Gelen yeni verilerle Cache'i güncelle (Merge işlemi)
                        foreach (var item in incomingList)
                        {
                            // AddOrUpdate: Varsa güncelle, yoksa ekle
                            _priceCache.AddOrUpdate(item.Symbol, item, (key, oldValue) => item);
                        }

                        // 2. Tenantlara yayını CACHE'deki TÜM VERİLERLE yap (Sadece gelenle değil!)
                        // Böylece Dolar değişmese bile cache'den son fiyatı gider.
                        await BroadcastToTenants();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Veri işleme hatası: {ex.Message}");
                }
            });

            try
            {
                await _client.ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Socket.IO Bağlanırken hata oluştu: {ex.Message}");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            if (_client.Connected) await _client.DisconnectAsync();
        }

        // --- GÜNCELLENMİŞ HESAPLAMA MOTORU ---
        // Artık parametre almıyor, sınıfın içindeki _priceCache'i kullanıyor.
        private async Task BroadcastToTenants()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var settingsService = scope.ServiceProvider.GetRequiredService<ITenantSettingsService>();
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

                var activeTenantIds = await tenantService.GetActiveTenantIdsAsync();
                if (activeTenantIds == null || !activeTenantIds.Any()) return;

                // Cache'teki son durumu al
                var latestMarketData = _priceCache.Values.ToDictionary(x => x.Symbol, x => x);

                foreach (var tenantId in activeTenantIds)
                {
                    var myProducts = await settingsService.GetProductsAsync(tenantId);
                    if (myProducts == null || !myProducts.Any()) continue;

                    var preparedList = new List<PriceData>();

                    foreach (var product in myProducts)
                    {
                        // Artık "latestMarketData" içinde arıyoruz.
                        // Dolar o an değişmemiş olsa bile cache'te durduğu için buradan gelecek.
                        if (latestMarketData.TryGetValue(product.SourceKey, out var sourceData))
                        {
                            decimal finalBid = 0;
                            decimal finalAsk = 0;

                            if (sourceData.Bid.HasValue)
                                finalBid = (sourceData.Bid.Value * product.BuyMultiplier) - product.AddonAmount;

                            if (sourceData.Ask.HasValue)
                                finalAsk = (sourceData.Ask.Value * product.SellMultiplier) + product.AddonAmount;

                            preparedList.Add(new PriceData
                            {
                                Symbol = product.Name,
                                Category = product.TenantProductCategory?.Name ?? "Genel",
                                Description = product.Name,
                                Source = "Sarfiyum",
                                Timestamp = sourceData.Timestamp,
                                Bid = finalBid > 0 ? finalBid : null,
                                Ask = finalAsk > 0 ? finalAsk : null
                            });
                        }
                    }

                    if (preparedList.Any())
                    {
                        await _hubContext.Clients.Group(tenantId).SendAsync("ReceivePrices", preparedList);
                    }
                }
            }
        }
    }
}