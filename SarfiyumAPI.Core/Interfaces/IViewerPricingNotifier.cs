using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    /// <summary>
    /// Bir viewer'ın özel fiyat ayarı değiştiğinde, o viewer'ın canlı SignalR
    /// bağlantılarının hangi yayın grubunda olduğunu günceller.
    ///
    /// Neden gerekli: GoldHub grup üyeliğine SADECE bağlantı anında karar veriyordu.
    /// Kuyumcu, viewer bağlıyken ona özel fiyat tanımlarsa viewer hem standart grupta
    /// kalıyor hem de özel listeye giriyordu -> iki farklı fiyat listesi alıp ekran
    /// titriyordu. Tersi durumda (özel ayar silinince) hiçbir gruba ait olmadığı için
    /// hiç fiyat almıyordu. Bu servis, grup üyeliğini ayar değişimiyle senkron tutar.
    ///
    /// Not: Servis katmanı SignalR'ı tanımadığı için ISessionNotificationService ile
    /// aynı desen kullanılıyor; implementasyon API katmanındadır.
    /// </summary>
    public interface IViewerPricingNotifier
    {
        /// <summary>
        /// Viewer artık özel fiyata sahip: standart tenant grubundan çıkarılır,
        /// böylece sadece kendisine özel hesaplanmış listeyi alır.
        /// </summary>
        Task MoveToSpecialAsync(string tenantId, string visitorId);

        /// <summary>
        /// Viewer'ın özel fiyatı kalmadı: standart tenant grubuna geri alınır.
        /// </summary>
        Task MoveToStandardAsync(string tenantId, string visitorId);
    }
}
