using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Service.Services
{
    /// <inheritdoc cref="IActiveSessionCache"/>
    public class ActiveSessionCache : IActiveSessionCache
    {
        private readonly IRepository<UserActiveSession> _sessionRepo;
        private readonly IMemoryCache _cache;

        // Tablonun tek yazarı AuthService olduğu ve hem login hem logout önbelleği
        // noktasal olarak güncellediği için süre uzun tutulabilir. Yine de bir emniyet
        // payı olsun diye (DB'ye elle müdahale, ikinci bir container vb.) mutlak süre
        // veriliyor — bu, önbelleğin en fazla bu kadar süre bayat kalabileceği anlamına gelir.
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        // Oturum YOK bilgisini de önbelleğe alıyoruz; aksi halde çıkış yapmış ama
        // hâlâ istek gönderen bir istemci her seferinde DB'ye gitmeye devam ederdi.
        // null önbelleğe alınamadığı için "oturum yok" bu sentinel ile temsil ediliyor.
        private const string NoSessionSentinel = "__none__";

        public ActiveSessionCache(IRepository<UserActiveSession> sessionRepo, IMemoryCache cache)
        {
            _sessionRepo = sessionRepo;
            _cache = cache;
        }

        public async Task<string?> GetSessionIdAsync(string userId, ClientSource clientSource)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            var key = BuildKey(userId, clientSource);

            if (_cache.TryGetValue(key, out string? cached))
            {
                return cached == NoSessionSentinel ? null : cached;
            }

            var session = await _sessionRepo
                .Where(x => x.UserId == userId && x.ClientSource == clientSource)
                .FirstOrDefaultAsync();

            var sessionId = session?.SessionId;

            _cache.Set(key, sessionId ?? NoSessionSentinel, BuildOptions());

            return sessionId;
        }

        public void Set(string userId, ClientSource clientSource, string sessionId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId)) return;

            _cache.Set(BuildKey(userId, clientSource), sessionId, BuildOptions());
        }

        public void Remove(string userId, ClientSource clientSource)
        {
            if (string.IsNullOrEmpty(userId)) return;

            // Silmek yerine "oturum yok" yazıyoruz: çıkış yapmış istemci genelde birkaç
            // istek daha gönderir, bunların DB'ye inmesine gerek yok.
            _cache.Set(BuildKey(userId, clientSource), NoSessionSentinel, BuildOptions());
        }

        private static string BuildKey(string userId, ClientSource clientSource)
            => $"ActiveSession_{userId}_{clientSource}";

        private static MemoryCacheEntryOptions BuildOptions()
            => new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetPriority(CacheItemPriority.High);
    }
}
