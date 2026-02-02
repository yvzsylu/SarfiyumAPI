using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces; // IVisitorSettingService için
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
        private readonly IRepository<UserActiveSession> _sessionRepo;
        private readonly IVisitorSettingService _visitorSettingService; // 🔥 YENİ: Servis Eklendi

        public GoldHub(
            ILogger<GoldHub> logger,
            IRepository<UserActiveSession> sessionRepo,
            IVisitorSettingService visitorSettingService)
        {
            _logger = logger;
            _sessionRepo = sessionRepo;
            _visitorSettingService = visitorSettingService;
        }

        public override async Task OnConnectedAsync()
        {
            // ... (GÜVENLİK ve SÜRE KONTROLLERİ AYNEN KALIYOR) ...
            var userId = Context.User?.FindFirst("userId")?.Value;
            var tokenSessionId = Context.User?.FindFirst("SessionId")?.Value;
            var clientSourceStr = Context.User?.FindFirst("ClientSource")?.Value;

            // 1. Session ve Security Check (Mevcut kodların)
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tokenSessionId) && !string.IsNullOrEmpty(clientSourceStr))
            {
                if (Enum.TryParse<ClientSource>(clientSourceStr, out var clientSource))
                {
                    var activeSession = await _sessionRepo
                        .Where(x => x.UserId == userId && x.ClientSource == clientSource)
                        .FirstOrDefaultAsync();

                    if (activeSession == null || activeSession.SessionId != tokenSessionId)
                    {
                        _logger.LogWarning($"[Security] User {userId} bağlantısı reddedildi.");
                        Context.Abort();
                        return;
                    }
                }
            }

            // 2. Zamanlayıcı (Mevcut kodların)
            var expClaim = Context.User?.FindFirst("exp")?.Value;
            if (expClaim != null && long.TryParse(expClaim, out long expSeconds))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                var timeLeft = expDate - DateTimeOffset.UtcNow;
                if (timeLeft.TotalSeconds <= 0) { Context.Abort(); return; }

                var isViewer = Context.User?.IsInRole("Viewer") ?? false;
                if (isViewer) { _ = ScheduleDisconnect(timeLeft, Context.ConnectionAborted); }
            }

            // ========================================================================
            // 3. GRUPLAMA (BURASI DEĞİŞTİ)
            // ========================================================================
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;

            if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(userId))
            {
                // 🔥 KONTROL: Bu kullanıcının özel bir fiyat ayarı var mı?
                bool hasSpecial = await _visitorSettingService.HasSpecialSettingsAsync(userId);

                if (hasSpecial)
                {
                    // Özel ayarı varsa GENEL gruba eklemiyoruz.
                    // Bu kullanıcıya HostedService içinde Clients.User(userId) ile özel veri atacağız.
                    _logger.LogInformation($"User {userId} özel ayarlara sahip. Standart gruba eklenmedi.");
                }
                else
                {
                    // Özel ayarı yoksa STANDART gruba ekle.
                    // İsmini değiştirdik: "TenantId_Standard"
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"{tenantId}_Standard");
                }
            }
            else
            {
                Context.Abort();
                return;
            }

            // Admin işlemleri vs için User ID grubuna eklemeye devam edebiliriz
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }

        private async Task ScheduleDisconnect(TimeSpan delay, CancellationToken token)
        {
            try { await Task.Delay(delay, token); Context.Abort(); }
            catch { /* Ignore */ }
        }
    }
}