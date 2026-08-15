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
        private readonly IActiveSessionCache _activeSessionCache;
        private readonly IVisitorSettingService _visitorSettingService; // 🔥 YENİ: Servis Eklendi
        private readonly SarfiyumAPI.Services.IHubConnectionRegistry _connectionRegistry;
        private readonly IPriceFeedStateTracker _feedState;
        private readonly SarfiyumAPI.Services.IConnectSnapshotProvider _connectSnapshot;

        public GoldHub(
            ILogger<GoldHub> logger,
            IActiveSessionCache activeSessionCache,
            IVisitorSettingService visitorSettingService,
            SarfiyumAPI.Services.IHubConnectionRegistry connectionRegistry,
            IPriceFeedStateTracker feedState,
            SarfiyumAPI.Services.IConnectSnapshotProvider connectSnapshot)
        {
            _logger = logger;
            _activeSessionCache = activeSessionCache;
            _visitorSettingService = visitorSettingService;
            _connectionRegistry = connectionRegistry;
            _feedState = feedState;
            _connectSnapshot = connectSnapshot;
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
                    // Eskiden burada doğrudan DB sorgusu vardı; TV ekranlarının reconnect
                    // fırtınalarında veritabanını dövüyordu (AGENTS.md P6). Artık aynı
                    // önbellekli servis kullanılıyor.
                    var activeSessionId = await _activeSessionCache.GetSessionIdAsync(userId, clientSource);

                    if (activeSessionId == null || activeSessionId != tokenSessionId)
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
            // 3. GRUPLAMA — platform bazlı
            // ========================================================================
            // Görünürlük filtresi (ShowOnWeb / ShowOnMobile) artık sunucuda uygulandığı için
            // web ve mobil FARKLI listeler alır; dolayısıyla yayın grupları da platform
            // bazlıdır (AGENTS.md P4). Platform, token'daki ClientSource'tan gelir.
            var tenantId = Context.User?.FindFirst("TenantId")?.Value;
            var platform = ResolvePlatform(Context.User?.FindFirst("ClientSource")?.Value);

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
            {
                Context.Abort();
                return;
            }

            // Bu kullanıcının özel bir fiyat ayarı var mı?
            bool hasSpecial = await _visitorSettingService.HasSpecialSettingsAsync(userId);

            var priceGroup = hasSpecial
                ? IPriceFeedStateTracker.VisitorGroup(userId, platform)
                : IPriceFeedStateTracker.StandardGroup(tenantId, platform);

            await Groups.AddToGroupAsync(Context.ConnectionId, priceGroup);

            // Kişiye özel sinyal grubu (ForceCheckSession, logout vb.).
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            // Bağlantıyı kaydet: özel fiyat ayarı değiştiğinde grup üyeliğini canlı
            // güncelleyebilmek ve bağlı olmayan viewer için boşa hesap yapmamak için gerekli.
            _connectionRegistry.Add(userId, Context.ConnectionId, platform);

            await base.OnConnectedAsync();

            // ========================================================================
            // 4. BAĞLANTI ANINDA TAM LİSTE
            // ========================================================================
            // Delta yayınının ön koşulu: istemci ilk olarak tam listeyi almalı, aksi halde
            // eline geçen ilk delta'yı boş bir listeye uygulamaya çalışır. Grup henüz hiç
            // yayın almadıysa null döner — o durumda bir sonraki tick zaten snapshot gönderir.
            //
            // İstemci sözleşmesi: snapshot gelmeden önce ulaşan delta'lar YOK SAYILIR,
            // ayrıca snapshot'takinden küçük/eşit `seq` taşıyan delta'lar da yok sayılır.
            // Bu, buradan giden mesaj ile gruba giden yayının sırasının garanti olmamasını
            // zararsız hale getirir.
            //
            // ⚠️ Burada eskiden yalnızca "daha önce yayınlanmış" durum okunuyordu; grup
            // soğuksa hiçbir şey gönderilmiyor ve ekran "Veri bekleniyor" yazıp kalıyordu.
            // Piyasa sakinken yayın döngüsü hiç çalışmadığı için bu bekleyiş süresiz
            // olabiliyordu. Artık grup soğuksa liste bağlantı anında hesaplanıyor.
            var snapshot = await _connectSnapshot.GetOrBuildAsync(
                priceGroup, tenantId, platform, hasSpecial ? userId : null);

            if (snapshot != null)
            {
                await Clients.Caller.SendAsync("ReceivePriceSnapshot", snapshot);
            }
        }

        /// <summary>
        /// Token'daki <c>ClientSource</c> (Web=1 / Mobile=2) değerini yayın platformuna çevirir.
        /// Okunamazsa Web varsayılır — eski istemciler için en güvenli taraf.
        /// </summary>
        private static PricePlatform ResolvePlatform(string? clientSourceClaim)
        {
            if (Enum.TryParse<ClientSource>(clientSourceClaim, out var source) && source == ClientSource.Mobile)
            {
                return PricePlatform.Mobile;
            }

            return PricePlatform.Web;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _connectionRegistry.Remove(userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task ScheduleDisconnect(TimeSpan delay, CancellationToken token)
        {
            try { await Task.Delay(delay, token); Context.Abort(); }
            catch { /* Ignore */ }
        }
    }
}