using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Service.Services
{
    /// <inheritdoc cref="IPriceCalculator"/>
    public class PriceCalculator : IPriceCalculator
    {
        private readonly ILogger<PriceCalculator> _logger;

        /// <summary>
        /// Fiyatlar tel üzerinde bu hane sayısına yuvarlanır.
        ///
        /// Çarpanlar <c>decimal(18,10)</c> olduğu için çarpım sonucunun ondalık hanesi
        /// toplanıyor ve JSON'a <c>5688.7300000000000000</c> gibi anlamsız bir kuyrukla
        /// yazılıyordu — satır başına ~30 byte boşuna. İstemciler zaten en fazla 3 hane
        /// gösteriyor; 4 hane rahat bir emniyet payı bırakır.
        /// </summary>
        private const int PriceDecimals = 4;

        // Hatalı konfigürasyon uyarısı ürün başına throttle edilir; aksi halde tek bir
        // yanlış ürün her yayın turunda satır üretir (AGENTS.md P9).
        private static readonly TimeSpan PriceConfigWarningInterval = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, DateTime> _lastPriceConfigWarningAt = new();

        public PriceCalculator(ILogger<PriceCalculator> logger)
        {
            _logger = logger;
        }

        public List<PriceItemDto> Calculate(
            IReadOnlyList<TenantProduct> products,
            IReadOnlyDictionary<string, PriceData> marketData,
            IReadOnlyList<VisitorProductSetting>? specialSettings,
            PricePlatform platform)
        {
            var list = new List<PriceItemDto>(products.Count);

            // Özel ayarlar ürün başına lineer aranıyordu (ürün × ayar). Tek seferlik
            // indeks aramayı sabit zamana indiriyor. Aynı ürün için birden fazla satır
            // beklenmez; olursa ilki geçerli sayılır.
            Dictionary<string, VisitorProductSetting>? settingsByProductId = null;
            if (specialSettings != null && specialSettings.Count > 0)
            {
                settingsByProductId = new Dictionary<string, VisitorProductSetting>(specialSettings.Count);
                foreach (var setting in specialSettings)
                {
                    settingsByProductId.TryAdd(setting.TenantProductId, setting);
                }
            }

            foreach (var product in products)
            {
                if (!IsVisibleOn(product, platform)) continue;

                if (!marketData.TryGetValue(product.SourceKey, out var sourceData)) continue;

                decimal buyMult = product.BuyMultiplier;
                decimal sellMult = product.SellMultiplier;
                decimal addon = product.AddonAmount;

                if (settingsByProductId != null &&
                    settingsByProductId.TryGetValue(product.Id, out var special))
                {
                    if (special.SpecialBuyMultiplier.HasValue) buyMult = special.SpecialBuyMultiplier.Value;
                    if (special.SpecialSellMultiplier.HasValue) sellMult = special.SpecialSellMultiplier.Value;
                    if (special.SpecialAddonAmount.HasValue) addon = special.SpecialAddonAmount.Value;
                }

                // Fiyat SADECE kaynak veri yoksa null gider. Eskiden "hesap sonucu <= 0 ise
                // null" deniyordu; bu, yanlış çarpan/makas girilen bir üründe hatayı sessizce
                // gizleyip istemcide boş/0 gösteriyordu (AGENTS.md M4). Artık hesap sonucu ne
                // çıkarsa o gönderilir ve anormal durum loglanır.
                decimal? finalBid = sourceData.Bid.HasValue
                    ? Round((sourceData.Bid.Value * buyMult) - addon)
                    : null;

                decimal? finalAsk = sourceData.Ask.HasValue
                    ? Round((sourceData.Ask.Value * sellMult) + addon)
                    : null;

                if (finalBid <= 0 || finalAsk <= 0)
                {
                    WarnAboutPriceConfigOnce(product, finalBid, finalAsk, buyMult, sellMult, addon);
                }

                list.Add(new PriceItemDto
                {
                    Id = product.Id,
                    Symbol = product.Name,
                    SourceKey = product.SourceKey,
                    Category = product.TenantProductCategory?.Name ?? "Genel",
                    CategoryIndex = product.TenantProductCategory?.OrderIndex ?? 0,
                    Bid = finalBid,
                    Ask = finalAsk,
                    OrderIndex = product.OrderIndex
                });
            }

            return list;
        }

        private static bool IsVisibleOn(TenantProduct product, PricePlatform platform) => platform switch
        {
            PricePlatform.Web => product.ShowOnWeb,
            PricePlatform.Mobile => product.ShowOnMobile,
            // Public ekran şimdilik mobil görünürlüğünü izler. Ayrı bir "public'te göster"
            // ihtiyacı doğarsa TenantProduct.ShowOnPublic eklenmelidir (§11 açık soru 1).
            PricePlatform.Public => product.ShowOnMobile,
            _ => true
        };

        private static decimal Round(decimal value)
            => decimal.Round(value, PriceDecimals, MidpointRounding.AwayFromZero);

        private void WarnAboutPriceConfigOnce(
            TenantProduct product,
            decimal? finalBid,
            decimal? finalAsk,
            decimal buyMult,
            decimal sellMult,
            decimal addon)
        {
            var now = DateTime.UtcNow;
            var lastWarnedAt = _lastPriceConfigWarningAt.GetOrAdd(product.Id, DateTime.MinValue);

            if (now - lastWarnedAt < PriceConfigWarningInterval) return;

            _lastPriceConfigWarningAt[product.Id] = now;

            _logger.LogWarning(
                $"[PRICE_CONFIG] Ürün '{product.Name}' (Id: {product.Id}, Tenant: {product.TenantId}) " +
                $"için hesaplanan fiyat pozitif değil. Alış: {finalBid}, Satış: {finalAsk}. " +
                $"Çarpan/makas ayarları kontrol edilmeli (Buy: {buyMult}, Sell: {sellMult}, Addon: {addon}).");
        }
    }
}
