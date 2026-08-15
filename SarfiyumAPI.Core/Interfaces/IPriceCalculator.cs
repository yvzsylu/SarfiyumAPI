using System.Collections.Generic;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;

namespace SarfiyumAPI.Core.Interfaces
{
    /// <summary>
    /// Tenant ürünleri + ham piyasa verisi + (varsa) viewer'a özel ayarlardan
    /// istemciye gidecek fiyat listesini üretir.
    ///
    /// Neden ayrı bir servis: bu hesap eskiden <c>GoldPriceHostedService</c> ve
    /// <c>MockGoldPriceHostedService</c> içinde İKİ KOPYA hâlindeydi ve davranışları
    /// farklıydı — mock, karşılığı olmayan ürüne sahte 1000/1010 fiyat uyduruyor,
    /// gerçek servis ise ürünü listeden düşürüyordu. Sonuç: lokalde görünen bir ürün
    /// canlıda görünmeyebiliyordu (AGENTS.md M3). Artık tek kaynak burasıdır;
    /// mock'un sahte veri üretmesi çağrıdan ÖNCE, kendi katmanında yapılır.
    /// </summary>
    public interface IPriceCalculator
    {
        /// <summary>
        /// Verilen platform için görünür ürünlerin fiyat listesini üretir.
        /// Kaynak piyasa verisi bulunmayan ürünler listeye girmez.
        /// </summary>
        List<PriceItemDto> Calculate(
            IReadOnlyList<TenantProduct> products,
            IReadOnlyDictionary<string, PriceData> marketData,
            IReadOnlyList<VisitorProductSetting>? specialSettings,
            PricePlatform platform);
    }
}
