using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Hubs
{
    /// <summary>
    /// Girişsiz (public) fiyat ekranının bağlandığı hub.
    ///
    /// <para>
    /// <b>Neden ayrı bir hub:</b> <see cref="GoldHub"/> sınıf seviyesinde <c>[Authorize]</c> ve
    /// içinde oturum doğrulama, tek cihaz kuralı, viewer'a özel gruplama ve otomatik disconnect
    /// mantığı var. Orayı <c>[AllowAnonymous]</c> yapıp içeride ayrıştırmak, ürünün en hassas
    /// güvenlik akışını riske atardı. Bu hub tamamen izoledir: yalnızca tenant çözer, tek bir
    /// gruba abone eder ve mevcut listeyi gönderir. Başka hiçbir şey yapmaz — istemciden
    /// çağrılabilecek tek bir metodu bile yoktur.
    /// </para>
    ///
    /// <para>
    /// <b>Erişim kuralı:</b> bağlantı yalnızca <c>slug</c> query parametresi geçerli bir
    /// tenant'a çözülüyorsa kabul edilir. <c>ResolvePublicTenantIdAsync</c> aynı zamanda yetki
    /// kontrolüdür: tenant pasifse, aboneliği bitmişse veya public yayını kapalıysa null döner
    /// ve bağlantı reddedilir.
    /// </para>
    /// </summary>
    [AllowAnonymous]
    public class PublicPriceHub : Hub
    {
        private readonly ITenantService _tenantService;
        private readonly IPriceFeedStateTracker _feedState;
        private readonly ILogger<PublicPriceHub> _logger;

        public PublicPriceHub(
            ITenantService tenantService,
            IPriceFeedStateTracker feedState,
            ILogger<PublicPriceHub> logger)
        {
            _tenantService = tenantService;
            _feedState = feedState;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var slug = Context.GetHttpContext()?.Request.Query["slug"].ToString();

            if (string.IsNullOrWhiteSpace(slug))
            {
                Context.Abort();
                return;
            }

            var tenantId = await _tenantService.ResolvePublicTenantIdAsync(slug);

            if (tenantId == null)
            {
                // Geçersiz/kapalı slug. Anonim uç nokta olduğu için ayrıntı sızdırmıyoruz;
                // istemci yalnızca bağlantının kurulmadığını görür.
                _logger.LogDebug($"[PUBLIC_HUB] Geçersiz slug ile bağlantı reddedildi.");
                Context.Abort();
                return;
            }

            var groupName = IPriceFeedStateTracker.StandardGroup(tenantId, PricePlatform.Public);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await base.OnConnectedAsync();

            // Bağlantı anında tam liste. Grup henüz hiç yayın almadıysa null döner ve
            // istemci bir sonraki tick'te zaten snapshot alır.
            var snapshot = _feedState.GetCurrentSnapshot(groupName);
            if (snapshot != null)
            {
                await Clients.Caller.SendAsync("ReceivePriceSnapshot", snapshot);
            }
        }
    }
}
