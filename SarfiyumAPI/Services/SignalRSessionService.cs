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

        public SignalRSessionService(IHubContext<GoldHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendForceCheckSession(string userId, string sessionId, string clientSource)
        {
            // AuthService'den gelen emri SignalR üzerinden istemciye iletir
            await _hubContext.Clients.Group(userId).SendAsync("ForceCheckSession", sessionId, clientSource);
        }
    }
}