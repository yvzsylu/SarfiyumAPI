using System.Collections.Concurrent;
using SarfiyumAPI.Core.Enums;

namespace SarfiyumAPI.Services
{
    /// <summary>
    /// Hangi kullanıcının hangi SignalR ConnectionId'lerine sahip olduğunu ve her
    /// bağlantının hangi platformdan geldiğini tutar.
    ///
    /// Bir kullanıcının aynı anda birden fazla bağlantısı olabilir (örn. mağaza TV'si
    /// + tarayıcı sekmesi) ve tek cihaz kuralı Web/Mobil için AYRI işlediğinden aynı
    /// viewer hem web hem mobilde bağlı olabilir. Yayın grupları platform bazlı olduğu
    /// için (görünürlük filtresi sunucuda uygulanıyor), bağlantının platformunu bilmek
    /// zorundayız.
    ///
    /// Singleton olarak kaydedilir. Tek container varsayımıyla çalışır; yatay
    /// ölçekleme yapılırsa SignalR backplane (Redis) ile birlikte ele alınmalıdır.
    /// </summary>
    public interface IHubConnectionRegistry
    {
        void Add(string userId, string connectionId, PricePlatform platform);
        void Remove(string userId, string connectionId);

        /// <summary>Kullanıcının açık bağlantıları. Bağlantı yoksa boş dizi döner.</summary>
        IReadOnlyCollection<string> GetConnections(string userId);

        /// <summary>
        /// Kullanıcının o an bağlı olduğu platformlar. Yayıncı, yalnızca gerçekten
        /// bağlanılmış platformlar için liste hesaplar (AGENTS.md P8).
        /// </summary>
        IReadOnlyCollection<PricePlatform> GetConnectedPlatforms(string userId);

        /// <summary>
        /// Bağlantı kimlikleri ve platformları birlikte. Grup üyeliği güncellerken
        /// hangi bağlantının hangi platform grubuna ait olduğunu bilmek gerekir.
        /// </summary>
        IReadOnlyCollection<(string ConnectionId, PricePlatform Platform)> GetConnectionDetails(string userId);
    }

    public class HubConnectionRegistry : IHubConnectionRegistry
    {
        // Değer tarafı da ConcurrentDictionary: HashSet thread-safe değil.
        // ConnectionId -> o bağlantının platformu.
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PricePlatform>> _connections = new();

        public void Add(string userId, string connectionId, PricePlatform platform)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId)) return;

            var set = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, PricePlatform>());
            set[connectionId] = platform;
        }

        public void Remove(string userId, string connectionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(connectionId)) return;

            if (_connections.TryGetValue(userId, out var set))
            {
                set.TryRemove(connectionId, out _);

                // Son bağlantı da kapandıysa kullanıcıyı sözlükten düşür (bellek sızıntısı olmasın).
                if (set.IsEmpty)
                {
                    _connections.TryRemove(userId, out _);
                }
            }
        }

        public IReadOnlyCollection<string> GetConnections(string userId)
        {
            if (!string.IsNullOrEmpty(userId) && _connections.TryGetValue(userId, out var set))
            {
                return set.Keys.ToArray();
            }

            return Array.Empty<string>();
        }

        public IReadOnlyCollection<PricePlatform> GetConnectedPlatforms(string userId)
        {
            if (!string.IsNullOrEmpty(userId) && _connections.TryGetValue(userId, out var set))
            {
                return set.Values.Distinct().ToArray();
            }

            return Array.Empty<PricePlatform>();
        }

        public IReadOnlyCollection<(string ConnectionId, PricePlatform Platform)> GetConnectionDetails(string userId)
        {
            if (!string.IsNullOrEmpty(userId) && _connections.TryGetValue(userId, out var set))
            {
                return set.Select(kvp => (kvp.Key, kvp.Value)).ToArray();
            }

            return Array.Empty<(string, PricePlatform)>();
        }
    }
}
