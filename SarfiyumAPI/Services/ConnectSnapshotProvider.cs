using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Services
{
    /// <summary>
    /// Yeni bağlanan bir istemciye gönderilecek tam fiyat listesini üretir.
    ///
    /// <para>
    /// <b>Çözdüğü hata (soğuk grup):</b> hub'lar eskiden yalnızca
    /// <c>IPriceFeedStateTracker.GetCurrentSnapshot</c> çağırıyordu; bu da grup DAHA ÖNCE
    /// yayın almışsa dolu, almamışsa <c>null</c> dönüyordu. <c>null</c> durumunda bağlanan
    /// istemciye hiçbir şey gönderilmiyor, "bir sonraki tick nasılsa yayın yapar"
    /// varsayılıyordu. Bu varsayım iki halde tutmuyor:
    /// </para>
    /// <list type="number">
    /// <item><b>Piyasa sakinken yayın döngüsü hiç çalışmaz.</b> Döngü yalnızca yeni piyasa
    /// verisi geldiğinde bir tur atar; veri akmıyorsa hiçbir grup ısınmaz. 60 saniyelik tam
    /// senkron da bu turun içinde olduğu için o da tetiklenmez. Sonuç: kapalı piyasada
    /// bağlanan herkes süresiz "Veri bekleniyor" görür.</item>
    /// <item><b>Sunucu her yeniden başladığında tüm grup durumu sıfırlanır</b> (bellekte
    /// tutuluyor). Her deploy'dan sonra aynı durum yaşanır.</item>
    /// </list>
    /// <para>
    /// Ayrıca viewer'a özel fiyat grupları <b>tanım gereği</b> soğuktur: yayıncı, yalnızca o
    /// an BAĞLI olan viewer'lar için liste hesaplar; viewer ise ilk kez bağlandığı anda
    /// henüz kayıtlı değildir. Yani özel fiyatlı bir viewer'ın ilk bağlantısı her zaman
    /// boş dönüyordu.
    /// </para>
    /// <para>
    /// Buradaki çözüm listeyi bağlantı anında hesaplamaktır. Hesap, yayıncıyla <b>aynı</b>
    /// <see cref="IPriceCalculator"/> üzerinden yapılır — ikinci bir fiyat mantığı kopyası
    /// oluşmaz (AGENTS.md M3). Üretilen durum tracker'a işlenir, böylece bir sonraki yayın
    /// turu doğru delta'yı üretir.
    /// </para>
    /// </summary>
    public interface IConnectSnapshotProvider
    {
        /// <summary>
        /// Grubun güncel tam listesini döner. Grup soğuksa liste anında hesaplanır.
        /// Gönderilecek bir şey yoksa (piyasa verisi hiç gelmemiş, tenant'ın ürünü yok,
        /// hiçbir ürün bu platformda görünmüyor) <c>null</c> döner.
        /// </summary>
        /// <param name="visitorId">
        /// Özel fiyatı olan viewer'ın kimliği; standart yayın için <c>null</c>.
        /// </param>
        Task<PriceSnapshotDto?> GetOrBuildAsync(
            string groupName,
            string tenantId,
            PricePlatform platform,
            string? visitorId = null);
    }

    /// <inheritdoc cref="IConnectSnapshotProvider"/>
    public class ConnectSnapshotProvider : IConnectSnapshotProvider
    {
        private readonly IPriceFeedStateTracker _feedState;
        private readonly IMarketDataCache _marketData;
        private readonly IPriceCalculator _priceCalculator;
        private readonly IMultiplierSettingsService _settingsService;
        private readonly IVisitorSettingService _visitorService;
        private readonly ILogger<ConnectSnapshotProvider> _logger;

        public ConnectSnapshotProvider(
            IPriceFeedStateTracker feedState,
            IMarketDataCache marketData,
            IPriceCalculator priceCalculator,
            IMultiplierSettingsService settingsService,
            IVisitorSettingService visitorService,
            ILogger<ConnectSnapshotProvider> logger)
        {
            _feedState = feedState;
            _marketData = marketData;
            _priceCalculator = priceCalculator;
            _settingsService = settingsService;
            _visitorService = visitorService;
            _logger = logger;
        }

        public async Task<PriceSnapshotDto?> GetOrBuildAsync(
            string groupName,
            string tenantId,
            PricePlatform platform,
            string? visitorId = null)
        {
            // Grup zaten sıcaksa hazır durumu kullan: hem daha ucuz, hem de yayıncının
            // ürettiği sıra numarasıyla birebir tutarlı.
            var existing = _feedState.GetCurrentSnapshot(groupName);
            if (existing != null) return existing;

            var marketData = _marketData.Snapshot();
            if (marketData.Count == 0)
            {
                // Sunucu yeni ayağa kalkmış ve piyasadan henüz tek bir paket bile gelmemiş.
                // Yapabileceğimiz bir şey yok; ilk paket geldiğinde yayın turu grubu ısıtır.
                _logger.LogDebug($"[CONNECT_SNAPSHOT] {groupName}: piyasa verisi henüz yok.");
                return null;
            }

            var products = await _settingsService.GetProductsAsync(tenantId);
            if (products == null || products.Count == 0) return null;

            IReadOnlyList<VisitorProductSetting>? specialSettings = null;

            if (!string.IsNullOrEmpty(visitorId))
            {
                var visitors = await _visitorService.GetVisitorsWithSpecialSettingsAsync(tenantId);
                specialSettings = visitors
                    .FirstOrDefault(v => v.VisitorId == visitorId)?
                    .Settings;
            }

            var items = _priceCalculator.Calculate(products, marketData, specialSettings, platform);
            if (items.Count == 0) return null;

            // Durumu tracker'a işle. Bu çağrı grubu "gönderildi" olarak işaretler, böylece
            // bir sonraki yayın turu sıfırdan snapshot değil, doğru delta üretir.
            _feedState.BuildNextMessage(groupName, items, DateTime.UtcNow.ToString("O"));

            // Araya yayıncı girmiş olabilir. Ne olursa olsun grubun O ANKİ doğru durumunu
            // döndürüyoruz — böylece istemcinin aldığı sıra numarası her zaman tutarlı olur.
            return _feedState.GetCurrentSnapshot(groupName);
        }
    }
}
