using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ISessionNotificationService
    {
        Task SendForceCheckSession(string userId, string sessionId, string clientSource);

        /// <summary>
        /// Kullanıcının açık SignalR bağlantılarını yayın gruplarından çıkarır.
        ///
        /// Neden gerekli: çıkış yapmak yalnızca token'ı silmek ve DB'deki oturum satırını
        /// düşürmekten ibaret olsaydı, açık kalan hub bağlantısı fiyat almaya devam ederdi —
        /// grup üyeliğine yalnızca bağlantı anında karar veriliyor. <c>ForceCheckSession</c>
        /// sinyali istemcinin kendini atmasını sağlar, bu metot ise istemci sinyali
        /// yok saysa bile akışı sunucu tarafında keser (AGENTS.md G10).
        /// </summary>
        Task DetachFromBroadcastsAsync(string userId, string tenantId);
    }
}
