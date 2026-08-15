using System.Threading.Tasks;
using SarfiyumAPI.Core.Enums;

namespace SarfiyumAPI.Core.Interfaces
{
    /// <summary>
    /// "Bu kullanıcının bu istemci tipindeki geçerli SessionId'si nedir?" sorusunun
    /// önbellekli cevabı.
    ///
    /// Neden gerekli: <c>SessionCheckMiddleware</c> her authenticated istekte, <c>GoldHub</c>
    /// ise her bağlantıda <c>UserActiveSessions</c> tablosuna gidiyordu. Statik dosyalar
    /// hariç tüm API çağrılarına fazladan bir DB gidiş-dönüşü ekleniyordu
    /// (AGENTS.md P5 ve P6). Tablonun tek yazarı <c>AuthService</c> (login + logout)
    /// olduğu için önbellek noktasal olarak geçersizleştirilebiliyor.
    ///
    /// Tek cihaz kuralı bozulmaz: yeni giriş yapıldığında <see cref="Set"/> önbelleği
    /// YENİ SessionId ile günceller, böylece eski cihazın bir sonraki isteği anında
    /// uyuşmazlığa düşer. Çıkışta <see cref="Remove"/> ile kayıt düşürülür.
    /// </summary>
    public interface IActiveSessionCache
    {
        /// <summary>
        /// Geçerli SessionId'yi döner. Kullanıcının bu istemci tipinde açık oturumu
        /// yoksa <c>null</c> döner. Önbellekte yoksa veritabanından okur ve önbelleğe alır.
        /// </summary>
        Task<string?> GetSessionIdAsync(string userId, ClientSource clientSource);

        /// <summary>Giriş sonrası yeni SessionId'yi önbelleğe yazar.</summary>
        void Set(string userId, ClientSource clientSource, string sessionId);

        /// <summary>Çıkış sonrası önbellek kaydını düşürür.</summary>
        void Remove(string userId, ClientSource clientSource);
    }
}
