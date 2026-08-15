using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;

namespace SarfiyumAPI.Services
{
    /// <inheritdoc cref="IViewerPricingNotifier"/>
    public class ViewerPricingNotifier : IViewerPricingNotifier
    {
        private readonly IHubContext<GoldHub> _hubContext;
        private readonly IHubConnectionRegistry _registry;
        private readonly IPriceFeedStateTracker _feedState;
        private readonly ILogger<ViewerPricingNotifier> _logger;

        public ViewerPricingNotifier(
            IHubContext<GoldHub> hubContext,
            IHubConnectionRegistry registry,
            IPriceFeedStateTracker feedState,
            ILogger<ViewerPricingNotifier> logger)
        {
            _hubContext = hubContext;
            _registry = registry;
            _feedState = feedState;
            _logger = logger;
        }

        public Task MoveToSpecialAsync(string tenantId, string visitorId)
            => MoveAsync(tenantId, visitorId, toSpecial: true);

        public Task MoveToStandardAsync(string tenantId, string visitorId)
            => MoveAsync(tenantId, visitorId, toSpecial: false);

        /// <summary>
        /// Viewer'ın açık bağlantılarını standart yayın grubu ile kendi özel grubu arasında
        /// taşır. Gruplar platform bazlı olduğu için (AGENTS.md P4) her bağlantı KENDİ
        /// platformunun grubuna taşınır; aynı viewer web ve mobilde aynı anda bağlı olabilir.
        /// </summary>
        private async Task MoveAsync(string tenantId, string visitorId, bool toSpecial)
        {
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(visitorId)) return;

            var connections = _registry.GetConnectionDetails(visitorId);

            // Viewer o an bağlı değilse yapacak bir şey yok:
            // bir sonraki bağlantısında GoldHub.OnConnectedAsync doğru grubu zaten seçecek.
            if (connections.Count == 0) return;

            foreach (var (connectionId, platform) in connections)
            {
                var standardGroup = IPriceFeedStateTracker.StandardGroup(tenantId, platform);
                var specialGroup = IPriceFeedStateTracker.VisitorGroup(visitorId, platform);

                var leaving = toSpecial ? standardGroup : specialGroup;
                var joining = toSpecial ? specialGroup : standardGroup;

                try
                {
                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, leaving);
                    await _hubContext.Groups.AddToGroupAsync(connectionId, joining);

                    // Yeni gruptaki mevcut durumu hemen gönder. Aksi halde istemci, o grup
                    // için bir sonraki SNAPSHOT'a kadar (en fazla 60 sn) elindeki eski
                    // listeyle kalırdı — delta'lar farklı bir listeye ait olduğu için
                    // uygulanamazdı.
                    var snapshot = _feedState.GetCurrentSnapshot(joining);
                    if (snapshot != null)
                    {
                        await _hubContext.Clients.Client(connectionId)
                            .SendAsync("ReceivePriceSnapshot", snapshot);
                    }
                }
                catch (Exception ex)
                {
                    // Bağlantı bu arada kopmuş olabilir; fiyat ayarı kaydını bozmamalı.
                    _logger.LogWarning($"[VIEWER_GROUP] {visitorId} için grup güncellenemedi ({connectionId}): {ex.Message}");
                }
            }

            _logger.LogInformation(
                $"[VIEWER_GROUP] {visitorId} -> {(toSpecial ? "ÖZEL" : "STANDART")} " +
                $"({connections.Count} bağlantı güncellendi).");
        }
    }
}
