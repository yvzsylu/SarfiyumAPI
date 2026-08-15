using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;

namespace SarfiyumAPI.Services
{
    /// <summary>Bir yayın turunun sonucu — yalnızca loglama için.</summary>
    public readonly record struct BroadcastStats(int SentMessages, int SkippedOfflineVisitors);

    /// <summary>
    /// Bir fiyat yayın turunu baştan sona yürüten TEK yer.
    ///
    /// <para>
    /// <b>Neden tek yer:</b> bu mantık daha önce <c>GoldPriceHostedService</c> ve
    /// <c>MockGoldPriceHostedService</c> içinde iki kopya hâlindeydi ve kopyalar zamanla
    /// birbirinden ayrıştı (AGENTS.md M3). Sonuç: <b>lokalde test edilen davranış canlıdaki
    /// davranış değildi.</b> Artık her iki ortam da bu sınıftan geçer; mock'un tek farkı
    /// piyasa verisini nereden aldığıdır.
    /// </para>
    /// </summary>
    public interface IPriceBroadcaster
    {
        Task<BroadcastStats> BroadcastAsync(IReadOnlyDictionary<string, PriceData> marketData);
    }

    /// <inheritdoc cref="IPriceBroadcaster"/>
    public class PriceBroadcaster : IPriceBroadcaster
    {
        private readonly IHubContext<GoldHub> _hubContext;
        private readonly IHubContext<PublicPriceHub> _publicHubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubConnectionRegistry _connectionRegistry;
        private readonly IPriceCalculator _priceCalculator;
        private readonly IPriceFeedStateTracker _feedState;

        /// <summary>Oturum açmış istemcilere üretilen platform listeleri.</summary>
        private static readonly PricePlatform[] StandardPlatforms =
            { PricePlatform.Web, PricePlatform.Mobile };

        public PriceBroadcaster(
            IHubContext<GoldHub> hubContext,
            IHubContext<PublicPriceHub> publicHubContext,
            IServiceProvider serviceProvider,
            IHubConnectionRegistry connectionRegistry,
            IPriceCalculator priceCalculator,
            IPriceFeedStateTracker feedState)
        {
            _hubContext = hubContext;
            _publicHubContext = publicHubContext;
            _serviceProvider = serviceProvider;
            _connectionRegistry = connectionRegistry;
            _priceCalculator = priceCalculator;
            _feedState = feedState;
        }

        public async Task<BroadcastStats> BroadcastAsync(IReadOnlyDictionary<string, PriceData> marketData)
        {
            var sent = 0;
            var skippedOfflineVisitors = 0;

            using var scope = _serviceProvider.CreateScope();

            var settingsService = scope.ServiceProvider.GetRequiredService<IMultiplierSettingsService>();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
            var visitorService = scope.ServiceProvider.GetRequiredService<IVisitorSettingService>();

            var activeTenantIds = await tenantService.GetActiveTenantIdsAsync();
            if (activeTenantIds == null || activeTenantIds.Count == 0)
            {
                return new BroadcastStats(0, 0);
            }

            // Girişsiz yayını açık olan tenant'lar. Bayrağı kapalı olanlar için public liste
            // hiç hesaplanmaz — kapalı tenant'a sıfır ek maliyet.
            var publicTenantIds = await tenantService.GetPublicFeedTenantIdsAsync();

            var timestamp = DateTime.UtcNow.ToString("O");
            var tasks = new List<Task>();

            foreach (var tenantId in activeTenantIds)
            {
                var products = await settingsService.GetProductsAsync(tenantId);
                if (products == null || products.Count == 0) continue;

                // Standart yayın: platform başına AYRI liste. Görünürlük filtresi sunucuda
                // uygulandığı için (AGENTS.md P4) web ve mobil farklı ürün kümesi görebilir.
                foreach (var platform in StandardPlatforms)
                {
                    var groupName = IPriceFeedStateTracker.StandardGroup(tenantId, platform);
                    var items = _priceCalculator.Calculate(products, marketData, null, platform);

                    if (Enqueue(tasks, _hubContext.Clients.Group(groupName), groupName, items, timestamp)) sent++;
                }

                // Girişsiz yayın. Tenant'ın STANDART çarpanlarını kullanır; viewer'a özel
                // ayar burada asla uygulanmaz — public ekran kimseye ait değildir.
                if (publicTenantIds.Contains(tenantId))
                {
                    var groupName = IPriceFeedStateTracker.StandardGroup(tenantId, PricePlatform.Public);
                    var items = _priceCalculator.Calculate(products, marketData, null, PricePlatform.Public);

                    if (Enqueue(tasks, _publicHubContext.Clients.Group(groupName), groupName, items, timestamp)) sent++;
                }

                // Viewer'a özel fiyatlar.
                var specialVisitors = await visitorService.GetVisitorsWithSpecialSettingsAsync(tenantId);

                foreach (var visitor in specialVisitors)
                {
                    // AGENTS.md P8: özel fiyatı tanımlı ama o an BAĞLI OLMAYAN viewer için
                    // liste hesaplamanın karşılığı yok. Yalnızca gerçekten bağlanılmış
                    // platformlar için hesap yapılır.
                    var connectedPlatforms = _connectionRegistry.GetConnectedPlatforms(visitor.VisitorId);

                    if (connectedPlatforms.Count == 0)
                    {
                        skippedOfflineVisitors++;
                        continue;
                    }

                    foreach (var platform in connectedPlatforms)
                    {
                        var groupName = IPriceFeedStateTracker.VisitorGroup(visitor.VisitorId, platform);
                        var items = _priceCalculator.Calculate(products, marketData, visitor.Settings, platform);

                        if (Enqueue(tasks, _hubContext.Clients.Group(groupName), groupName, items, timestamp)) sent++;
                    }
                }
            }

            if (tasks.Count > 0) await Task.WhenAll(tasks);

            return new BroadcastStats(sent, skippedOfflineVisitors);
        }

        /// <summary>
        /// Grup için gönderilecek bir şey varsa mesajı kuyruğa ekler. Snapshot mı delta mı
        /// gideceğine <see cref="IPriceFeedStateTracker"/> karar verir; hiçbir fiyat
        /// oynamadıysa mesaj hiç üretilmez.
        /// </summary>
        private bool Enqueue(
            List<Task> tasks,
            IClientProxy group,
            string groupName,
            List<PriceItemDto> items,
            string timestamp)
        {
            if (items.Count == 0) return false;

            var message = _feedState.BuildNextMessage(groupName, items, timestamp);
            if (message == null) return false;

            tasks.Add(message.IsSnapshot
                ? group.SendAsync("ReceivePriceSnapshot", message.Snapshot)
                : group.SendAsync("ReceivePriceDelta", message.Delta));

            return true;
        }
    }
}
