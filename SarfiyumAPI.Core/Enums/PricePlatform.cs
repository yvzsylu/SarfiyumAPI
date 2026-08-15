namespace SarfiyumAPI.Core.Enums
{
    /// <summary>
    /// Fiyat listesinin hangi istemci için üretildiğini belirtir.
    ///
    /// Görünürlük filtresi (<c>ShowOnWeb</c> / <c>ShowOnMobile</c>) eskiden İSTEMCİDE
    /// uygulanıyordu: gizlenmiş ürünlerin fiyatları da tel üzerinden gidiyor, Angular ve
    /// Flutter bunları alıp siliyordu. Hem gereksiz trafik hem gizlilik sızıntısıydı
    /// (AGENTS.md P4). Artık filtre sunucuda uygulanır ve platform başına ayrı yayın
    /// grubu kullanılır.
    /// </summary>
    public enum PricePlatform
    {
        Web = 1,
        Mobile = 2,

        /// <summary>
        /// Girişsiz (public) fiyat ekranı. Tenant'ın standart çarpanlarını kullanır,
        /// viewer'a özel ayar uygulanmaz. Şimdilik mobil görünürlüğüyle aynı kabul edilir.
        /// </summary>
        Public = 3
    }
}
