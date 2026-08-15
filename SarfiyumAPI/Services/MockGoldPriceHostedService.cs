using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Services
{
    /// <summary>
    /// LOKAL geliştirme/test için sahte piyasa verisi üretir.
    ///
    /// <para>
    /// <b>Amaç canlıyı birebir taklit etmektir.</b> Datshop yalnızca canlı sunucunun IP'sini
    /// whitelist'e aldığı için lokalde gerçek veri akmaz; bu servis onun yerine geçer.
    /// Yayının zamanlaması, snapshot/delta kararı, platform grupları, viewer override'ları ve
    /// public yayın <b>tamamen ortak koddan</b> geçer (<see cref="PriceFeedHostedServiceBase"/>
    /// + <see cref="IPriceBroadcaster"/>). Bu sınıfın tek işi "Datshop paketi geldi" demektir.
    /// </para>
    ///
    /// <para>
    /// Eskiden yayın mantığı burada ayrı bir kopyaydı ve gerçek servisten farklıydı — lokalde
    /// görünen ürün canlıda görünmeyebiliyordu (AGENTS.md M3). Artık böyle bir sapma mümkün değil.
    /// </para>
    ///
    /// <para><b>Yapılandırma</b> (<c>appsettings.Development.json</c> → <c>MockFeed</c>):</para>
    /// <list type="bullet">
    /// <item><c>PacketIntervalMs</c> — paket üretim aralığı (varsayılan 400 ms).
    /// Datshop da saniyede birkaç paket gönderir; bu sayede tick'in paketleri birleştirmesi
    /// (coalescing) lokalde de gerçekten test edilir.</item>
    /// <item><c>Paused</c> — <c>true</c> ise hiç paket üretilmez. "Piyasa kapalı" senaryosunu
    /// ve "yeni veri yoksa yayın yapılmaz" davranışını test etmek için.</item>
    /// <item><c>Volatility</c> — fiyat oynaklığı çarpanı (varsayılan 1.0). Büyütmek flash
    /// efektlerini ve delta trafiğini artırır.</item>
    /// </list>
    /// </summary>
    public class MockGoldPriceHostedService : PriceFeedHostedServiceBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MockGoldPriceHostedService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Random _random = new(20260815); // sabit tohum: tekrarlanabilir testler

        /// <summary>Sembol → o an geçerli alış/satış.</summary>
        private readonly Dictionary<string, (decimal Bid, decimal Ask)> _current = new();

        /// <summary>
        /// Gerçekçi başlangıç fiyatları. Buradaki değerler canlıdan alınmış makul
        /// büyüklüklerdir; amaç ekranda "doğru mertebede" sayılar görmektir.
        /// </summary>
        private static readonly Dictionary<string, (decimal Bid, decimal Ask)> BasePrices = new()
        {
            // --- Madenler ---
            ["ALTIN"] = (6253.55m, 6292.94m),
            ["KULCEALTIN"] = (6222.28m, 6311.82m),
            ["GUMTRY"] = (99.1625m, 104.2916m),
            ["GUMUSD"] = (2.3125m, 2.3540m),
            ["AYAR22"] = (5688.73m, 5958.29m),
            ["AYAR14"] = (3441.99m, 4590.41m),
            ["XAUUSD"] = (4560.20m, 4562.80m),
            ["XAGUSD"] = (52.410m, 52.660m),
            ["XAUXAG"] = (86.85m, 87.15m),
            ["XAUEUR"] = (3877.40m, 3880.10m),
            ["XPTUSD"] = (1418.50m, 1423.00m),
            ["XPDUSD"] = (1179.00m, 1184.00m),
            ["PLATIN"] = (1948.00m, 1972.00m),
            ["PALADYUM"] = (1615.00m, 1638.00m),

            // --- Gram altınlar ---
            ["5 Gr Gram Altın"] = (31267.75m, 31464.70m),
            ["10 Gr Gram Altın"] = (62535.50m, 62929.40m),
            ["20 Gr Gram Altın"] = (125071.00m, 125858.80m),
            ["50 Gr Gram Altın"] = (312677.50m, 314647.00m),
            ["100 Gr Gram Altın"] = (625355.00m, 629294.00m),

            // --- Ziynet (yeni) ---
            ["CEYREK_YENI"] = (10024m, 10281m),
            ["YARIM_YENI"] = (20030m, 20556m),
            ["TEK_YENI"] = (40085m, 40924m),
            ["ATA_YENI"] = (41742m, 42076m),
            ["ATA5_YENI"] = (207618m, 209920m),
            ["GREMESE_YENI"] = (100057m, 101996m),

            // --- Ziynet (eski) ---
            ["CEYREK_ESKI"] = (10006m, 10111m),
            ["YARIM_ESKI"] = (20011m, 20192m),
            ["TEK_ESKI"] = (40085m, 40484m),
            ["ATA_ESKI"] = (41649m, 41994m),
            ["ATA5_ESKI"] = (206993m, 208976m),
            ["GREMESE_ESKI"] = (100057m, 101366m),

            // --- Döviz / TRY ---
            ["USDTRY"] = (42.8754m, 42.9789m),
            ["EURTRY"] = (50.4108m, 50.6348m),
            ["GBPTRY"] = (57.9120m, 58.1450m),
            ["CHFTRY"] = (53.6480m, 53.9010m),
            ["AUDTRY"] = (28.5120m, 28.6940m),
            ["CADTRY"] = (31.2050m, 31.3880m),
            ["SARTRY"] = (11.4310m, 11.4980m),
            ["JPYTRY"] = (0.28910m, 0.29140m),
            ["NOKTRY"] = (4.05200m, 4.07600m),
            ["SEKTRY"] = (4.47900m, 4.50300m),
            ["DKKTRY"] = (6.75800m, 6.79200m),
            ["XUSDTRY"] = (42.8500m, 42.9600m),

            // --- Çapraz kurlar ---
            ["EURUSD"] = (1.17580m, 1.17640m),
            ["GBPUSD"] = (1.35080m, 1.35150m),
            ["USDCHF"] = (0.79910m, 0.79970m),
            ["USDCAD"] = (1.37410m, 1.37480m),
            ["AUDUSD"] = (0.66510m, 0.66570m),
            ["USDSAR"] = (3.75040m, 3.75090m),
            ["USDJPY"] = (148.310m, 148.380m),
            ["USDNOK"] = (10.5820m, 10.5910m),
            ["USDSEK"] = (9.57300m, 9.58100m),
            ["USDDKK"] = (6.34200m, 6.34800m),
            ["USDRUB"] = (79.4800m, 79.6200m),
            ["EURGBP"] = (0.87030m, 0.87090m),
            ["EURCHF"] = (0.93960m, 0.94020m),
            ["EURUSDS"] = (1.17550m, 1.17670m),
            ["XAUUSDS"] = (4559.80m, 4563.40m),
            ["KXAGUSD"] = (52.380m, 52.700m),

            // --- Efektif / paralel ---
            ["PARUSD"] = (42.8100m, 43.0400m),
            ["PAREUR"] = (50.3400m, 50.7100m),
            ["PARGBP"] = (57.8300m, 58.2300m),
            ["PARCHF"] = (53.5700m, 53.9800m),

            // --- Fark / vade kalemleri (küçük değerler) ---
            ["FARK"] = (0.0250m, 0.0310m),
            ["FARKEUR"] = (0.0280m, 0.0350m),
            ["VADE FARK"] = (0.0190m, 0.0240m),
        };

        public MockGoldPriceHostedService(
            IPriceBroadcaster broadcaster,
            IConfiguration configuration,
            ILogger<MockGoldPriceHostedService> logger,
            IServiceProvider serviceProvider,
            IMarketDataCache marketData)
            : base(broadcaster, configuration, logger, marketData)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛠️ MOCK fiyat kaynağı başlıyor (Datshop simülasyonu).");

            await SeedSymbolsAsync(stoppingToken);

            // Gerçek servisle aynı yapı: veri üretimi ile yayın döngüsü AYRI çalışır.
            // Böylece "paket sıklığı" ile "yayın sıklığı" arasındaki fark lokalde de gerçektir.
            await Task.WhenAll(
                RunPacketLoopAsync(stoppingToken),
                RunBroadcastLoopAsync(stoppingToken));
        }

        /// <summary>
        /// Sembolleri veritabanındaki <c>SystemCatalog</c>'dan okur — yani canlıda Datshop'un
        /// gönderdiği sembol kümesinin aynısı. Böylece kuyumcunun tanımlayabileceği HER ürün
        /// için lokalde de veri akar; "lokalde ürün fiyatsız görünüyor" durumu oluşmaz.
        /// </summary>
        private async Task SeedSymbolsAsync(CancellationToken stoppingToken)
        {
            List<string> catalogKeys = new();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IRepository<SystemCatalog>>();
                catalogKeys = await repo.Where(x => true).Select(x => x.SourceKey).ToListAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[MOCK] SystemCatalog okunamadı, yalnızca gömülü liste kullanılacak: {ex.Message}");
            }

            var allKeys = new HashSet<string>(BasePrices.Keys);
            foreach (var key in catalogKeys)
            {
                if (!string.IsNullOrWhiteSpace(key)) allKeys.Add(key);
            }

            var derived = new List<string>();

            foreach (var key in allKeys)
            {
                if (BasePrices.TryGetValue(key, out var known))
                {
                    _current[key] = known;
                }
                else
                {
                    // Katalogda olup gerçekçi değerini bilmediğimiz sembol. Makul bir sayı
                    // üretiyoruz ama bunu AÇIKÇA loglıyoruz — test sırasında bu ürünlerin
                    // rakamlarına anlam yüklenmesin.
                    var seed = Math.Abs(key.GetHashCode() % 4000) + 100;
                    _current[key] = (seed, seed * 1.012m);
                    derived.Add(key);
                }
            }

            _logger.LogInformation(
                $"[MOCK] {_current.Count} sembol hazır ({BasePrices.Count} gerçekçi, {derived.Count} türetilmiş).");

            if (derived.Count > 0)
            {
                _logger.LogInformation(
                    $"[MOCK] Türetilmiş (rakamları temsili) semboller: {string.Join(", ", derived)}");
            }

            // İlk paketi hemen bas ki ekranlar boş açılmasın.
            EmitPacket(1.0);
        }

        private async Task RunPacketLoopAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMilliseconds(
                _configuration.GetValue<int?>("MockFeed:PacketIntervalMs") ?? 400);

            var volatility = _configuration.GetValue<double?>("MockFeed:Volatility") ?? 1.0;

            _logger.LogInformation(
                $"[MOCK] Paket üretimi {interval.TotalMilliseconds:F0} ms aralıkla, oynaklık x{volatility:F2}.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(interval, stoppingToken); }
                catch (OperationCanceledException) { break; }

                // Duraklatılmışsa hiç paket üretme: "piyasa kapalı" senaryosu.
                // Yayın döngüsünün boşa dönmesi ve hiç mesaj gitmemesi böyle test edilir.
                if (_configuration.GetValue<bool?>("MockFeed:Paused") == true) continue;

                EmitPacket(volatility);
            }
        }

        /// <summary>
        /// Bir "Datshop paketi" üretir: sembollerin bir kısmının fiyatı oynar.
        /// Gerçekte de her pakette tüm semboller değişmez; bu yüzden rastgele bir alt küme
        /// seçiliyor — delta yayınının gerçekten kısmi çalıştığı lokalde de görülsün.
        /// </summary>
        private void EmitPacket(double volatility)
        {
            var keys = _current.Keys.ToList();
            if (keys.Count == 0) return;

            // Sembollerin ~%35'i bu pakette oynasın.
            var changeCount = Math.Max(1, keys.Count * 35 / 100);
            var packet = new List<PriceData>(changeCount);
            var now = DateTime.Now.ToString("O");

            for (var i = 0; i < changeCount; i++)
            {
                var key = keys[_random.Next(keys.Count)];
                var (bid, ask) = _current[key];

                // Oynaklık fiyatın büyüklüğüyle orantılı olsun (binde ~0.4).
                var step = bid * 0.0004m * (decimal)volatility;
                var delta = (decimal)(_random.NextDouble() * 2 - 1) * step;

                bid += delta;
                ask += delta;

                _current[key] = (bid, ask);

                packet.Add(new PriceData
                {
                    Symbol = key,
                    Bid = decimal.Round(bid, 6),
                    Ask = decimal.Round(ask, 6),
                    Timestamp = now,
                    Source = "MOCK"
                });
            }

            // Gerçek servisteki `price_changed` handler'ıyla aynı çağrı.
            UpdatePrices(packet);
        }

        /// <summary>
        /// Kuyumcu, katalogda olmayan bir <c>SourceKey</c> girmiş olabilir. Canlıda böyle bir
        /// ürün fiyat almaz ve listede görünmez; mock'ta da AYNI davranış geçerlidir.
        /// Burada kasıtlı olarak sahte değer uydurulmaz — aksi halde lokal ile canlı ayrışırdı.
        /// </summary>
        protected override IReadOnlyDictionary<string, PriceData> PrepareMarketData() => base.PrepareMarketData();
    }
}
