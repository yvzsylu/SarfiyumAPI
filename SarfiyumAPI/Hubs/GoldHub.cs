using Microsoft.AspNetCore.Authorization; // Authorize için
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SarfiyumAPI.Hubs
{
    [Authorize] // 🔥 Sadece Token'ı olanlar bağlanabilsin
    public class GoldHub : Hub
    {
        private readonly ILogger<GoldHub> _logger;

        public GoldHub(ILogger<GoldHub> logger)
        {
            _logger = logger;
        }

        // Kullanıcı bağlandığı an çalışır
        public override async Task OnConnectedAsync()
        {
            // Token içindeki "TenantId" claim'ini bul
            // Not: Senin JWT oluştururken kullandığın claim ismi önemli. 
            // Genelde "tenantId" veya "TenantId" olur.
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrEmpty(tenantId))
            {
                // Kullanıcıyı otomatik olarak kendi tenant ID'siyle oluşturulmuş gruba ekle
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
                _logger.LogInformation($"Kullanıcı {Context.ConnectionId}, '{tenantId}' grubuna eklendi.");
            }
            else
            {
                _logger.LogWarning("Bağlanan kullanıcının Token'ında TenantId bulunamadı!");
            }

            await base.OnConnectedAsync();
        }

        // Manuel metoda artık gerek kalmadı, silebilirsin.
        /* public async Task JoinTenantGroup(string tenantId) { ... } 
        */
    }
}