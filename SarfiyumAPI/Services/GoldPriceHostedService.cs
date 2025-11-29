using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // ToListAsync için
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;     // Tenant entity'si için
using SarfiyumAPI.Core.Interfaces;   // IRepository ve ITenantSettingsService için
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

            // --- SOCKET.IO KONFİGÜRASYONU ---
            const string serverUrl = "https://sse.datshop.com.tr";
            const string myKey = "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";

            _client = new SocketIOClient.SocketIO(serverUrl, new SocketIOOptions
            {
                Auth = new { socket_key = myKey },
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("socket_key", myKey)
                },
                Transport = TransportProtocol.Polling | TransportProtocol.WebSocket,
                EIO = SocketIO.Core.EngineIO.V4,
                Reconnection = true,
                ReconnectionDelay = 1000
            });

            // --- EVENT HANDLERS ---
            _client.OnConnected += async (sender, e) =>
            {
                _logger.LogInformation($"✅ Socket.IO Bağlantısı Başarılı! (Socket ID: {_client.Id})");
            };

            _client.OnDisconnected += (sender, e) =>
            {
                _logger.LogWarning($"❌ Socket.IO Bağlantısı Koptu: {e}");
            };

            _client.On("price_changed", async response =>
            {
                try
                {
                    var dataList = response.GetValue<List<PriceData>>();
                    if (dataList != null && dataList.Any())
                    {
                        await BroadcastToTenants(dataList);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Veri işleme hatası: {ex.Message}");
                }
            });

            // --- BAŞLATMA ---
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

            if (_client.Connected)
            {
                await _client.DisconnectAsync();
            }
        }

        // --- HESAPLAMA VE DAĞITIM MOTORU ---
        private async Task BroadcastToTenants(List<PriceData> rawData)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                // 1. Gerekli Servisleri Çağır
                var settingsService = scope.ServiceProvider.GetRequiredService<ITenantSettingsService>();

                // 🔥 DÜZELTME: IDataContext yerine IRepository<Tenant> çağırıyoruz
                var tenantRepository = scope.ServiceProvider.GetRequiredService<IRepository<Tenant>>();

                // 2. 🔥 AKTİF TENANTLARI REPOSITORY'DEN ÇEK 🔥
                // Repository'nin 'Where' metodunu kullanarak aktif ve silinmemiş olanları filtreliyoruz.
                var activeTenantIds = await tenantRepository
                    .Where(t => t.IsActive && !t.IsDeleted)
                    .Select(t => t.Id)
                    .ToListAsync();

                // Eğer hiç aktif tenant yoksa boşuna işlem yapma
                if (!activeTenantIds.Any()) return;

                // 3. Her Tenant İçin Hesaplama Yap
                foreach (var tenantId in activeTenantIds)
                {
                    // Cache'ten çarpanları getir
                    var multipliers = await settingsService.GetMultipliersAsync(tenantId);

                    var processedData = new List<PriceData>();

                    foreach (var rawItem in rawData)
                    {
                        decimal buyCoef = 1.0m;
                        decimal sellCoef = 1.0m;
                        decimal addon = 0m;

                        if (multipliers.TryGetValue(rawItem.Symbol, out var setting))
                        {
                            buyCoef = setting.BuyMultiplier;
                            sellCoef = setting.SellMultiplier;
                            addon = setting.AddonAmount;
                        }

                        processedData.Add(new PriceData
                        {
                            Symbol = rawItem.Symbol,
                            Category = rawItem.Category,
                            Description = rawItem.Description,
                            Source = "Sarfiyum",
                            Timestamp = rawItem.Timestamp,

                            // 🔥 FİYAT FORMÜLÜ
                            Bid = rawItem.Bid.HasValue ? (rawItem.Bid.Value * buyCoef) - addon : null,
                            Ask = rawItem.Ask.HasValue ? (rawItem.Ask.Value * sellCoef) + addon : null
                        });
                    }

                    // 4. Sinyali Gönder
                    await _hubContext.Clients.Group(tenantId).SendAsync("ReceivePrices", processedData);
                }
            }
        }
    }
}