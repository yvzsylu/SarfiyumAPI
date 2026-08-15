using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Enums;
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
    /// <summary>
    /// CANLI veri kaynağı: Datshop Socket.IO. Yayının zamanlaması ve içeriği
    /// <see cref="PriceFeedHostedServiceBase"/> ile <see cref="IPriceBroadcaster"/> tarafından
    /// yürütülür; bu sınıf yalnızca soketi yönetir ve gelen paketleri önbelleğe yazar.
    /// </summary>
    public class GoldPriceHostedService : PriceFeedHostedServiceBase
    {
        private readonly ILogger<GoldPriceHostedService> _logger;
        private SocketIOClient.SocketIO _client;
        private readonly IConfiguration _configuration;

        // Son verinin geldiği zamanı tutar (Watchdog için)
        private DateTime _lastDataReceivedTime = DateTime.UtcNow;

        // --- Watchdog varsayılanları (AGENTS.md P10) ---
        // Eski davranış: 15 saniye veri gelmezse soketi zorla yeniden kur. Piyasa kapalıyken
        // (gece/hafta sonu) Datshop veri göndermediği için bu, sonsuz reconnect döngüsüne ve
        // log şişmesine yol açıyordu. Artık eşik daha yüksek ve ardışık sıfırlamalarda
        // üstel geri çekilme (backoff) uygulanıyor.
        private const int DefaultWatchdogIdleSeconds = 60;
        private const int DefaultWatchdogMaxBackoffSeconds = 300;
        private static readonly TimeSpan WatchdogPollInterval = TimeSpan.FromSeconds(5);

        // Veri akışı kesildiğinde art arda kaç kez sıfırlama denendiğini tutar.
        // Gerçek veri geldiği an sıfırlanır.
        private int _consecutiveWatchdogResets;

        public GoldPriceHostedService(
            IPriceBroadcaster broadcaster,
            IConfiguration configuration,
            ILogger<GoldPriceHostedService> logger)
            : base(broadcaster, configuration, logger)
        {
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

            _client.OnConnected += (sender, e) => _logger.LogInformation("✅ [SOCKET] Bağlantı Başarılı!");

            _client.OnDisconnected += (sender, e) => _logger.LogWarning($"[SOCKET] Bağlantı koptu. Sebep: {e}");
            // Yeniden bağlanma denemeleri kopukluk sürdükçe saniyede bir tetiklenebilir → Debug.
            // Kopmanın kendisi zaten yukarıda Warning olarak bir kez loglanıyor.
            _client.OnReconnectAttempt += (sender, e) => _logger.LogDebug($"[SOCKET] Yeniden bağlanılıyor. Deneme: {e}");
            _client.OnError += (sender, e) => _logger.LogError($"[SOCKET] Hata oluştu: {e}");

            _client.On("price_changed", async response =>
            {
                // Veri geldiği an kronometreyi sıfırla; watchdog sıfırlama serisi varsa sonlandır.
                _lastDataReceivedTime = DateTime.UtcNow;
                var resetsBeforeRecovery = Interlocked.Exchange(ref _consecutiveWatchdogResets, 0);
                if (resetsBeforeRecovery > 0)
                {
                    _logger.LogInformation(
                        $"[WATCHDOG] Veri akışı yeniden başladı ({resetsBeforeRecovery} sıfırlama denemesinden sonra).");
                }

                // AGENTS.md P1: Bu handler ARTIK YAYIN YAPMIYOR.
                // Eskiden her `price_changed` paketi tam bir BroadcastToTenants() tetikliyordu;
                // Datshop saniyede birkaç paket gönderdiği için tenant × ürün kadar hesap
                // saniyede birkaç kez tekrarlanıyordu ve bu, sunucunun en büyük CPU
                // tüketicisiydi. Artık paket yalnızca önbelleği günceller; yayını sabit
                // aralıklı bir tick döngüsü yapar (bkz. RunBroadcastLoopAsync).
                try
                {
                    var incomingList = response.GetValue<List<PriceData>>();
                    // Normal akış: pakette kaç ürün geldiği. Saniyede birkaç kez tetiklendiği için Debug.
                    _logger.LogDebug($"[SOCKET_IN] Veri paketi alındı. İçindeki ürün sayısı: {incomingList?.Count ?? 0}");
                    if (incomingList != null && incomingList.Any())
                    {
                        // Önbelleği güncelle ve bir sonraki tick'e iş bırak. Yayını taban
                        // sınıftaki döngü yapar — mock ile birebir aynı yol.
                        UpdatePrices(incomingList);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[BROADCAST_ERROR] Veri işleme hatası: {ex.Message}\nStack: {ex.StackTrace}");
                }

                await Task.CompletedTask;
            });

            try { await _client.ConnectAsync(); }
            catch (Exception ex) { _logger.LogError($"[SOCKET_ERROR] İlk Bağlantı Hatası: {ex.Message}"); }

            // İki bağımsız döngü: biri sabit aralıkla yayın yapar, diğeri soketi gözler.
            // Ayrı tutuluyorlar çünkü periyotları farklı ve biri diğerini bekletmemeli.
            await Task.WhenAll(
                RunBroadcastLoopAsync(stoppingToken),
                RunWatchdogLoopAsync(stoppingToken));

            if (_client.Connected) await _client.DisconnectAsync();
        }

        /// <summary>
        /// Watchdog (Bekçi) döngüsü.
        /// Soket "bağlı" görünüp veri akmadığı durumu (ghost connection) yakalar.
        /// Piyasa kapalıyken de veri akmadığı için, ardışık sıfırlamalarda üstel geri
        /// çekilme uygulanır; aksi halde gece boyunca dakikada birkaç kez reconnect
        /// denenir ve hem log hem karşı tarafta rate-limit riski doğar (AGENTS.md P10).
        /// </summary>
        private async Task RunWatchdogLoopAsync(CancellationToken stoppingToken)
        {
            var idleThreshold = TimeSpan.FromSeconds(
                _configuration.GetValue<int?>("SocketServer:WatchdogIdleSeconds") ?? DefaultWatchdogIdleSeconds);
            var maxBackoff = TimeSpan.FromSeconds(
                _configuration.GetValue<int?>("SocketServer:WatchdogMaxBackoffSeconds") ?? DefaultWatchdogMaxBackoffSeconds);

            var nextResetAllowedAt = DateTime.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(WatchdogPollInterval, stoppingToken);
                }
                catch (OperationCanceledException) { break; }

                var now = DateTime.UtcNow;

                // Geri çekilme süresi dolmadıysa hiç bakma.
                if (now < nextResetAllowedAt) continue;

                var idle = now - _lastDataReceivedTime;
                if (idle < idleThreshold) continue;

                var resetAttempt = Interlocked.Increment(ref _consecutiveWatchdogResets);

                // İlk sıfırlama gerçek bir anomali sinyalidir → Warning.
                // Ardışık sıfırlamalar büyük olasılıkla piyasanın kapalı olmasından
                // kaynaklanır; her birini Warning'e yazmak diski doldurur → Debug.
                if (resetAttempt == 1)
                {
                    _logger.LogWarning(
                        $"[WATCHDOG] {idle.TotalSeconds:F0} saniyedir Socket.IO verisi gelmiyor. Bağlantı sıfırlanıyor...");
                }
                else
                {
                    _logger.LogDebug(
                        $"[WATCHDOG] Veri akışı hâlâ yok ({idle.TotalSeconds:F0} sn). Sıfırlama denemesi: {resetAttempt}.");
                }

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
                    _logger.LogDebug("[WATCHDOG] Socket.IO bağlantısı zorla yenilendi.");
                }
                catch (Exception ex)
                {
                    // Bağlantı kurulamadıysa da geri çekilme uygulanır; aksi halde
                    // sunucu erişilemezken 5 saniyede bir hata logu üretilir.
                    _logger.LogError($"[WATCHDOG] Yeniden bağlanma başarısız: {ex.Message}");
                }

                // Kronometreyi sıfırla ki eşik yeniden baştan işlesin.
                _lastDataReceivedTime = DateTime.UtcNow;
                nextResetAllowedAt = DateTime.UtcNow + ComputeWatchdogBackoff(resetAttempt, idleThreshold, maxBackoff);
            }
        }

        /// <summary>
        /// Ardışık sıfırlama sayısına göre üstel geri çekilme süresi üretir:
        /// eşik × 2^(deneme-1), üst sınır <paramref name="maxBackoff"/>.
        /// (60 sn eşik için: 60 → 120 → 240 → 300 → 300 ...)
        /// </summary>
        private static TimeSpan ComputeWatchdogBackoff(int resetAttempt, TimeSpan idleThreshold, TimeSpan maxBackoff)
        {
            // Üs alma taşmasını önlemek için deneme sayısı sınırlanır.
            var exponent = Math.Min(resetAttempt - 1, 16);
            var seconds = idleThreshold.TotalSeconds * Math.Pow(2, exponent);

            return seconds >= maxBackoff.TotalSeconds
                ? maxBackoff
                : TimeSpan.FromSeconds(seconds);
        }


    }
}