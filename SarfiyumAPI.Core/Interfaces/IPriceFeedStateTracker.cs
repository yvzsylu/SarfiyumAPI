using System.Collections.Generic;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Enums;

namespace SarfiyumAPI.Core.Interfaces
{
    /// <summary>
    /// Yayın grubu başına "en son ne gönderildi" durumunu tutar ve bir sonraki tick'te
    /// tam liste mi (snapshot) yoksa sadece değişenler mi (delta) gönderileceğine karar verir.
    ///
    /// Tasarım kuralları:
    /// <list type="bullet">
    /// <item>Grup ilk kez yayın alıyorsa → snapshot.</item>
    /// <item>Ürün yapılandırması değiştiyse (ad, kategori, sıra, ürün eklendi/çıkarıldı,
    ///       görünürlük değişti) → snapshot. Bu, kuyumcu ürün ayarlarını değiştirdiğinde
    ///       istemcinin eski adlarla kalmasını engeller ve ayrı bir bildirim mekanizması
    ///       gerektirmez — fark zaten karşılaştırma sırasında görülür.</item>
    /// <item><see cref="ResyncInterval"/> geçtiyse → snapshot. İstemci ile sunucu arasında
    ///       herhangi bir sapma olursa en fazla bu kadar yaşar; kendiliğinden düzelir.</item>
    /// <item>Aksi halde → yalnızca fiyatı oynayan satırlar. Hiçbiri oynamadıysa hiç mesaj
    ///       gönderilmez.</item>
    /// </list>
    /// </summary>
    public interface IPriceFeedStateTracker
    {
        /// <summary>
        /// Grup için gönderilecek mesajı üretir. Gönderilecek bir şey yoksa <c>null</c> döner.
        /// Çağrı, grubun durumunu günceller (yani "gönderildi" kabul edilir).
        /// </summary>
        PriceFeedMessage? BuildNextMessage(string groupName, List<PriceItemDto> currentItems, string timestamp);

        /// <summary>
        /// Yeni bağlanan bir istemciye gönderilecek tam listeyi döner.
        /// Grup henüz hiç yayın almadıysa <c>null</c> döner — bu durumda istemci bir sonraki
        /// tick'te zaten snapshot alacaktır.
        /// Grubun sıra numarasını ilerletmez.
        /// </summary>
        PriceSnapshotDto? GetCurrentSnapshot(string groupName);

        /// <summary>Grup tamamen boşaldığında durumu bırakır (bellek sızıntısı olmasın).</summary>
        void Forget(string groupName);

        /// <summary>Yayın grubu adlarının tek kaynağı. Hub ve yayıncı aynı adı üretmek zorunda.</summary>
        static string StandardGroup(string tenantId, PricePlatform platform) => $"{tenantId}_Std_{platform}";

        /// <summary>Viewer'a özel fiyat grubu (platform bazlı).</summary>
        static string VisitorGroup(string visitorId, PricePlatform platform) => $"{visitorId}_{platform}";
    }

    /// <summary>Tracker'ın ürettiği mesaj: ya snapshot ya delta.</summary>
    public sealed class PriceFeedMessage
    {
        public PriceSnapshotDto? Snapshot { get; init; }
        public PriceDeltaDto? Delta { get; init; }

        public bool IsSnapshot => Snapshot != null;
    }
}
