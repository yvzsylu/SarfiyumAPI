using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Hubs;
using System.Threading.Tasks;

namespace SarfiyumAPI.Services
{
    // Bu sınıf API katmanında olduğu için Hub'ı tanır
    public class SignalRSessionService : ISessionNotificationService
    {
        private readonly IHubContext<GoldHub> _hubContext;
        private readonly IHubConnectionRegistry _connectionRegistry;
        private readonly ILogger<SignalRSessionService> _logger;

        public SignalRSessionService(
            IHubContext<GoldHub> hubContext,
            IHubConnectionRegistry connectionRegistry,
            ILogger<SignalRSessionService> logger)
        {
            _hubContext = hubContext;
            _connectionRegistry = connectionRegistry;
            _logger = logger;
        }

        public async Task SendForceCheckSession(string userId, string sessionId, string clientSource)
        {
            // AuthService'den gelen emri SignalR üzerinden istemciye iletir
            await _hubContext.Clients.Group(userId).SendAsync("ForceCheckSession", sessionId, clientSource);
        }

        /// <inheritdoc />
        public async Task DetachFromBroadcastsAsync(string userId, string tenantId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var connections = _connectionRegistry.GetConnectionDetails(userId);
            if (connections.Count == 0) return;

            foreach (var (connectionId, platform) in connections)
            {
                try
                {
                    // Kişiye özel sinyal grubu (ForceCheckSession buradan gider).
                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, userId);

                    // Fiyat yayın grupları platform bazlıdır; bağlantı hangisindeyse
                    // ikisini de düşürüyoruz (viewer özel gruba taşınmış olabilir).
                    await _hubContext.Groups.RemoveFromGroupAsync(
                        connectionId, IPriceFeedStateTracker.VisitorGroup(userId, platform));

                    if (!string.IsNullOrEmpty(tenantId))
                    {
                        await _hubContext.Groups.RemoveFromGroupAsync(
                            connectionId, IPriceFeedStateTracker.StandardGroup(tenantId, platform));
                    }
                }
                catch (Exception ex)
                {
                    // Bağlantı bu arada kopmuş olabilir; çıkış akışını bozmamalı.
                    _logger.LogWarning($"[LOGOUT] {userId} bağlantısı gruplardan çıkarılamadı ({connectionId}): {ex.Message}");
                }
            }

            _logger.LogInformation($"[LOGOUT] {userId} için {connections.Count} bağlantı yayın gruplarından çıkarıldı.");
        }
    }
}
