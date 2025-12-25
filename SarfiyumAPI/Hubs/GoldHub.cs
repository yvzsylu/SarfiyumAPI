using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading; // CancellationToken için
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SarfiyumAPI.Hubs
{
    [Authorize]
    public class GoldHub : Hub
    {
        private readonly ILogger<GoldHub> _logger;

        public GoldHub(ILogger<GoldHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            // --- 1. SÜRE KONTROLÜ VE ZAMANLAYICI ---
            var expClaim = Context.User?.FindFirst("exp")?.Value;

            if (expClaim != null && long.TryParse(expClaim, out long expSeconds))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                var timeLeft = expDate - DateTimeOffset.UtcNow;

                if (timeLeft.TotalSeconds <= 0)
                {
                    Context.Abort();
                    return;
                }

                // Sadece Viewer ise zamanlayıcıyı kur
                var isViewer = Context.User?.IsInRole("Viewer") ?? false;
                if (isViewer)
                {
                    // 🔥 OPTİMİZASYON: Context.ConnectionAborted token'ını gönderiyoruz.
                    // Kullanıcı 15 dk dolmadan interneti koparırsa veya çıkarsa,
                    // bu Task otomatik olarak iptal edilir ve RAM'den silinir.
                    _ = ScheduleDisconnect(timeLeft, Context.ConnectionAborted);
                }
            }

            // --- 2. GRUPLAMA ---
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
            }
            else
            {
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        // 🔥 Optimize Edilmiş Metot
        private async Task ScheduleDisconnect(TimeSpan delay, CancellationToken token)
        {
            try
            {
                // Kullanıcı bağlı kaldığı sürece bekle.
                // Eğer kullanıcı çıkarsa 'token' tetiklenir ve bu satır hata fırlatıp görevi bitirir.
                await Task.Delay(delay, token);

                // Süre doldu ve kullanıcı hala buradaysa:
                Context.Abort();
            }
            catch (TaskCanceledException)
            {
                // Kullanıcı süre dolmadan kendi isteğiyle çıktı.
                // Task iptal edildi, hiçbir şey yapmana gerek yok.
                // RAM temizlendi.
            }
            catch (Exception)
            {
                // Diğer olası hatalar (Context dispose vb.)
            }
        }
    }
}