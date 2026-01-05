using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // FirstOrDefaultAsync için gerekli
using Microsoft.Extensions.Logging;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SarfiyumAPI.Hubs
{
    [Authorize]
    public class GoldHub : Hub
    {
        private readonly ILogger<GoldHub> _logger;
        private readonly IRepository<UserActiveSession> _sessionRepo; // 🔥 YENİ

        public GoldHub(ILogger<GoldHub> logger, IRepository<UserActiveSession> sessionRepo)
        {
            _logger = logger;
            _sessionRepo = sessionRepo;
        }

        public override async Task OnConnectedAsync()
        {
            // ========================================================================
            // 1. GÜVENLİK KONTROLÜ (CONCURRENT SESSION)
            // ========================================================================
            // Token içindeki SessionId ile veritabanındaki SessionId tutuyor mu?
            // Tutmuyorsa, bu kullanıcı başka bir cihazdan giriş yapmış demektir -> ATILMALI.
            // ========================================================================

            var userId = Context.User?.FindFirst("userId")?.Value;
            var tokenSessionId = Context.User?.FindFirst("SessionId")?.Value;
            var clientSourceStr = Context.User?.FindFirst("ClientSource")?.Value;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tokenSessionId) && !string.IsNullOrEmpty(clientSourceStr))
            {
                if (Enum.TryParse<ClientSource>(clientSourceStr, out var clientSource))
                {
                    // Repository üzerinden kontrol et
                    // Not: Repository'in Where metodu IQueryable dönüyor, FirstOrDefaultAsync ile asenkron çekiyoruz.
                    var activeSession = await _sessionRepo
                        .Where(x => x.UserId == userId && x.ClientSource == clientSource)
                        .FirstOrDefaultAsync();

                    // Eğer DB'de kayıt yoksa veya SessionId'ler uyuşmuyorsa
                    if (activeSession == null || activeSession.SessionId != tokenSessionId)
                    {
                        _logger.LogWarning($"[Security] User {userId} bağlantısı reddedildi. Geçersiz SessionId.");
                        Context.Abort(); // 🔥 Bağlantıyı anında kopar
                        return;
                    }
                }
            }


            // ========================================================================
            // 2. SÜRE KONTROLÜ VE ZAMANLAYICI (MEVCUT MANTIK)
            // ========================================================================
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
                    _ = ScheduleDisconnect(timeLeft, Context.ConnectionAborted);
                }
            }


            // ========================================================================
            // 3. GRUPLAMA
            // ========================================================================
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

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }

        // 🔥 Optimize Edilmiş Metot
        private async Task ScheduleDisconnect(TimeSpan delay, CancellationToken token)
        {
            try
            {
                await Task.Delay(delay, token);
                Context.Abort();
            }
            catch (TaskCanceledException)
            {
                // Kullanıcı süre dolmadan çıktı, sorun yok.
            }
            catch (Exception)
            {
                // Olası diğer hatalar
            }
        }
    }
}