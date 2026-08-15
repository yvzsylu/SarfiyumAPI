using System.Collections.Generic;
using SarfiyumAPI.Core.DTOs;

namespace SarfiyumAPI.Core.Interfaces
{
    /// <summary>
    /// Piyasadan gelen en son ham fiyat durumu — süreç genelinde TEK kopya.
    ///
    /// <para>
    /// <b>Neden ayrı bir servis:</b> bu önbellek eskiden yalnızca yayın döngüsünün içindeydi
    /// (<c>PriceFeedHostedServiceBase</c>'in özel alanı). Dolayısıyla hub'lar, bir istemci
    /// bağlandığında fiyat listesini kendileri hesaplayamıyor, yalnızca daha önce yayınlanmış
    /// bir durumu tekrar gönderebiliyordu. Grup daha önce hiç yayın almadıysa bağlanan
    /// istemciye <b>hiçbir şey gitmiyordu</b> ve ekran "Veri bekleniyor" yazıp kalıyordu
    /// (AGENTS.md §5.x — soğuk grup hatası). Önbellek ortaklaştırılınca hub, bağlantı anında
    /// listeyi kendisi üretebiliyor.
    /// </para>
    ///
    /// <para>
    /// Yazan taraf yalnızca fiyat besleme servisidir; hub'lar yalnızca okur.
    /// </para>
    /// </summary>
    public interface IMarketDataCache
    {
        /// <summary>Tek bir sembolü günceller. Sembolü olmayan kayıt yok sayılır.</summary>
        void Update(PriceData item);

        /// <summary>Bir paketteki tüm fiyatları yazar. En az biri yazıldıysa <c>true</c> döner.</summary>
        bool UpdateRange(IEnumerable<PriceData> items);

        /// <summary>
        /// O anki durumun kopyası. Kopya döner ki okuyan taraf, yazan tarafla yarışmasın.
        /// Henüz hiç veri gelmediyse boş sözlük döner.
        /// </summary>
        IReadOnlyDictionary<string, PriceData> Snapshot();
    }
}
