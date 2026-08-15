using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface IAuthService
    {
        // Dönüş tipleri ServiceResult<T> ile sarıldı
        Task<ServiceResult<string>> LoginAsync(LoginDto model); // Token string döner
        Task<ServiceResult<string>> ResetPasswordAsync(string email);

        /// <summary>
        /// Oturumu sunucu tarafında kapatır: <c>UserActiveSession</c> satırını siler,
        /// oturum önbelleğini düşürür ve kullanıcının açık SignalR bağlantılarını
        /// yayın gruplarından çıkarır.
        ///
        /// <paramref name="sessionId"/> token'dan gelir ve DB'deki değerle karşılaştırılır:
        /// eski bir token'ın, aradan yapılmış YENİ bir girişin oturumunu kapatması engellenir.
        /// İşlem idempotenttir — zaten kapalı bir oturum için de başarı döner.
        /// </summary>
        Task<ServiceResult<string>> LogoutAsync(string userId, ClientSource clientSource, string sessionId, string? tenantId);

    }
}
