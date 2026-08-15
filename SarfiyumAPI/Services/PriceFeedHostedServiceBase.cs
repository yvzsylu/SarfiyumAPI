using System.Diagnostics;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Services
{
    /// <summary>
    /// Fiyat yayınının zamanlama iskeleti. Piyasa verisinin NEREDEN geldiğinden bağımsızdır.
    ///
    /// <para>
    /// <b>Tick tabanlı yayın (AGENTS.md P1):</b> veri kaynağı yalnızca <see cref="UpdatePrice"/>
    /// ile önbelleği günceller ve "kirli" işareti bırakır. Yayını sabit aralıklı bu döngü yapar.
    /// Böylece yayın frekansı, gelen paket frekansından bağımsızdır ve aradaki paketler doğal
    /// olarak birleştirilir. Yeni veri yoksa tick boşa döner — piyasa sakinken sıfır yayın.
    /// </para>
    ///
    /// <para>
    /// <b>Neden ortak sınıf:</b> gerçek ve mock servisler bu iskeleti paylaşmazsa, lokalde
    /// test edilen zamanlama davranışı canlıdakiyle aynı olmaz. Daha önce yayın mantığının
    /// iki kopya olması tam da bu tür bir sapmaya yol açmıştı (AGENTS.md M3).
    /// </para>
    /// </summary>
    public abstract class PriceFeedHostedServiceBase : BackgroundService
    {
        private const int DefaultBroadcastIntervalMs = 1000;

        private static readonly TimeSpan RepeatedWarningInterval = TimeSpan.FromMinutes(5);

        private readonly IPriceBroadcaster _broadcaster;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        /// <summary>
        /// Piyasa durumu artık burada DEĞİL, süreç genelinde ortak bir serviste tutuluyor.
        /// Sebebi: hub'ların da bağlantı anında fiyat listesi üretebilmesi gerekiyor
        /// (bkz. <see cref="IConnectSnapshotProvider"/>). Önbellek bu sınıfa özel kaldığı
        /// sürece, daha önce hiç yayın almamış bir gruba bağlanan istemciye hiçbir şey
        /// gönderilemiyordu.
        /// </summary>
        private readonly IMarketDataCache _marketData;

        private readonly SemaphoreSlim _broadcastLock = new(1, 1);

        private int _hasPendingUpdates;
        private int _skippedTickCount;
        private DateTime _lastOverlapWarningAt = DateTime.MinValue;

        protected PriceFeedHostedServiceBase(
            IPriceBroadcaster broadcaster,
            IConfiguration configuration,
            ILogger logger,
            IMarketDataCache marketData)
        {
            _broadcaster = broadcaster;
            _configuration = configuration;
            _logger = logger;
            _marketData = marketData;
        }

        /// <summary>Şu ana kadar toplanmış son piyasa durumu.</summary>
        protected IReadOnlyDictionary<string, PriceData> PriceCache => _marketData.Snapshot();

        /// <summary>
        /// Veri kaynağı yeni bir fiyat aldığında bunu çağırır. Yayın YAPMAZ; yalnızca
        /// önbelleği günceller ve bir sonraki tick'e iş bırakır.
        /// </summary>
        protected void UpdatePrice(PriceData item)
        {
            if (item?.Symbol == null) return;

            _marketData.Update(item);
            Interlocked.Exchange(ref _hasPendingUpdates, 1);
        }

        /// <summary>Birden fazla fiyatı tek seferde yazar (bir Datshop paketi gibi).</summary>
        protected void UpdatePrices(IEnumerable<PriceData> items)
        {
            if (_marketData.UpdateRange(items))
            {
                Interlocked.Exchange(ref _hasPendingUpdates, 1);
            }
        }

        /// <summary>
        /// Yayın turundan hemen önce çağrılır. Mock, kaynak verisi olmayan ürünler için
        /// sahte değer eklemek üzere bunu genişletir; gerçek serviste varsayılan davranış
        /// önbelleğin olduğu gibi kullanılmasıdır.
        /// </summary>
        protected virtual IReadOnlyDictionary<string, PriceData> PrepareMarketData()
            => _marketData.Snapshot();

        /// <summary>Sabit aralıklı yayın döngüsü.</summary>
        protected async Task RunBroadcastLoopAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMilliseconds(
                _configuration.GetValue<int?>("SocketServer:BroadcastIntervalMs") ?? DefaultBroadcastIntervalMs);

            _logger.LogInformation($"[BROADCAST] Yayın döngüsü {interval.TotalMilliseconds:F0} ms aralıkla başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(interval, stoppingToken); }
                catch (OperationCanceledException) { break; }

                // Yeni veri yoksa yayın yapma.
                if (Interlocked.Exchange(ref _hasPendingUpdates, 0) == 0) continue;

                // Önceki yayın hâlâ sürüyorsa bu tick'i atla. Bayrağı geri koyuyoruz ki veri
                // kaybolmasın; bir sonraki tick yine yayınlamayı deneyecek.
                if (!_broadcastLock.Wait(0))
                {
                    Interlocked.Exchange(ref _hasPendingUpdates, 1);
                    ReportSkippedTick();
                    continue;
                }

                var sw = Stopwatch.StartNew();

                try
                {
                    var stats = await _broadcaster.BroadcastAsync(PrepareMarketData());
                    sw.Stop();

                    if (sw.ElapsedMilliseconds > 1000)
                    {
                        _logger.LogWarning(
                            $"[PERF] Broadcast geç tamamlandı: {sw.ElapsedMilliseconds}ms. " +
                            $"Gönderim: {stats.SentMessages}, atlanan çevrimdışı viewer: {stats.SkippedOfflineVisitors}");
                    }
                    else
                    {
                        _logger.LogDebug(
                            $"[PERF] Broadcast tamamlandı: {sw.ElapsedMilliseconds}ms. " +
                            $"Gönderim: {stats.SentMessages}, atlanan çevrimdışı viewer: {stats.SkippedOfflineVisitors}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[BROADCAST_ERROR] Yayın hatası: {ex.Message}\nStack: {ex.StackTrace}");
                }
                finally
                {
                    _broadcastLock.Release();
                }
            }
        }

        /// <summary>
        /// Bir yayın tick'i, önceki yayın hâlâ sürdüğü için atlandığında sayar ve en fazla
        /// <see cref="RepeatedWarningInterval"/> aralıklarla toplu olarak loglar (AGENTS.md P9).
        ///
        /// Bu uyarı anlamlı bir sinyaldir: tick tabanlı yayında overlap, "bir yayın turu tick
        /// aralığından uzun sürüyor" demektir.
        /// </summary>
        private void ReportSkippedTick()
        {
            Interlocked.Increment(ref _skippedTickCount);

            var now = DateTime.UtcNow;
            if (now - _lastOverlapWarningAt < RepeatedWarningInterval) return;

            _lastOverlapWarningAt = now;
            var skipped = Interlocked.Exchange(ref _skippedTickCount, 0);

            _logger.LogWarning(
                $"[OVERLAP] Yayın turu tick aralığından uzun sürüyor. " +
                $"Son {RepeatedWarningInterval.TotalMinutes:F0} dakikada atlanan tick sayısı: {skipped}.");
        }
    }
}
