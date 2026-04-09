using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;
using SocketIOClient;
using SocketIOClient.Transport;
using SarfiyumAPI.Core.Entities;
using System.Linq;
using System.Threading;
using System.Diagnostics;

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

        private readonly SemaphoreSlim _broadcastLock = new SemaphoreSlim(1, 1);

        // 🔥 YENİ EKLENDİ: Son verinin geldiği zamanı tutar (Watchdog için)
        private DateTime _lastDataReceivedTime = DateTime.UtcNow;

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

            _client.OnConnected += async (sender, e) => _logger.LogInformation($"✅ [SOCKET] Bağlantı Başarılı!");

            _client.OnDisconnected += (sender, e) => _logger.LogWarning($"[SOCKET] Bağlantı koptu. Sebep: {e}");
            _client.OnReconnectAttempt += (sender, e) => _logger.LogInformation($"[SOCKET] Yeniden bağlanılıyor. Deneme: {e}");
            _client.OnError += (sender, e) => _logger.LogError($"[SOCKET] Hata oluştu: {e}");

            _client.On("price_changed", async response =>
            {
                // 🔥 YENİ EKLENDİ: Veri geldiği an kronometreyi sıfırla
                _lastDataReceivedTime = DateTime.UtcNow;

                if (!_broadcastLock.Wait(0))
                {
                    _logger.LogWarning("[OVERLAP] Önceki yayın devam ediyor, yeni paket atlandı.");
                    return;
                }

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
                    _logger.LogError($"[BROADCAST_ERROR] Veri işleme hatası: {ex.Message}\nStack: {ex.StackTrace}");
                }
                finally
                {
                    _broadcastLock.Release();
                }
            });

            try { await _client.ConnectAsync(); }
            catch (Exception ex) { _logger.LogError($"[SOCKET_ERROR] İlk Bağlantı Hatası: {ex.Message}"); }

            // 🔥 YENİ EKLENDİ: Watchdog (Bekçi) Döngüsü
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);

                var idleSeconds = (DateTime.UtcNow - _lastDataReceivedTime).TotalSeconds;

                // Eğer 15 saniyeden uzun süredir fiyat gelmiyorsa soket sessizce düşmüş (Ghost Connection) demektir
                if (idleSeconds > 15)
                {
                    _logger.LogWarning($"[WATCHDOG] {idleSeconds:F0} saniyedir Socket.IO verisi gelmiyor. Sessiz kopma tespit edildi, bağlantı sıfırlanıyor...");

                    try
                    {
                        if (_client.Connected)
                        {
                            await _client.DisconnectAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"[WATCHDOG] Kapanma sırasında beklenen hata: {ex.Message}");
                    }

                    await Task.Delay(1000, stoppingToken); // Bağlantının tam düşmesi için kısa bir bekleme

                    try
                    {
                        await _client.ConnectAsync();
                        _lastDataReceivedTime = DateTime.UtcNow; // Kronometreyi sıfırla ki ard üste tetiklenmesin
                        _logger.LogInformation("[WATCHDOG] Socket.IO bağlantısı zorla yenilendi.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[WATCHDOG] Yeniden bağlanma başarısız: {ex.Message}");
                    }
                }
            }

            if (_client.Connected) await _client.DisconnectAsync();
        }

        private async Task BroadcastToTenants()
        {
            var sw = Stopwatch.StartNew();
            int broadcastCount = 0;

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var settingsService = scope.ServiceProvider.GetRequiredService<IMultiplierSettingsService>();
                    var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
                    var visitorService = scope.ServiceProvider.GetRequiredService<IVisitorSettingService>();

                    var activeTenantIds = await tenantService.GetActiveTenantIdsAsync();
                    if (activeTenantIds == null || !activeTenantIds.Any()) return;

                    var latestMarketData = _priceCache.Values.ToDictionary(x => x.Symbol, x => x);

                    var broadcastTasks = new List<Task>();

                    foreach (var tenantId in activeTenantIds)
                    {
                        var myProducts = await settingsService.GetProductsAsync(tenantId);
                        if (myProducts == null || !myProducts.Any()) continue;

                        var standardList = CalculatePriceList(myProducts, latestMarketData, null);
                        if (standardList.Any())
                        {
                            broadcastTasks.Add(_hubContext.Clients.Group($"{tenantId}_Standard").SendAsync("ReceivePrices", standardList));
                            broadcastCount++;
                        }

                        var specialVisitors = await visitorService.GetOnlineVisitorsWithSettingsAsync(tenantId);
                        foreach (var visitorSession in specialVisitors)
                        {
                            var specialList = CalculatePriceList(myProducts, latestMarketData, visitorSession.Settings);
                            broadcastTasks.Add(_hubContext.Clients.Group(visitorSession.VisitorId).SendAsync("ReceivePrices", specialList));
                            broadcastCount++;
                        }
                    }

                    if (broadcastTasks.Any())
                    {
                        await Task.WhenAll(broadcastTasks);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[BROADCAST_DB_ERROR] Veritabanı veya gönderim aşamasında hata: {ex.Message}");
            }

            sw.Stop();
            if (sw.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning($"[PERF] Broadcast geç tamamlandı: {sw.ElapsedMilliseconds}ms. Gönderim sayısı: {broadcastCount}");
            }
            else
            {
                _logger.LogInformation($"[PERF] Broadcast tamamlandı: {sw.ElapsedMilliseconds}ms. Gönderim sayısı: {broadcastCount}");
            }
        }

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