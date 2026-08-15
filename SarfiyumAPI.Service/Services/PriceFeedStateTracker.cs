using System.Collections.Concurrent;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Service.Services
{
    /// <inheritdoc cref="IPriceFeedStateTracker"/>
    public class PriceFeedStateTracker : IPriceFeedStateTracker
    {
        /// <summary>
        /// Bu süre geçtiyse delta yerine tam liste gönderilir. Delta mimarisinin tek gerçek
        /// riski istemcinin listesinin sunucudan sapmasıdır; periyodik tam senkron bu riski
        /// "en fazla 60 saniye" ile sınırlar ve kendiliğinden düzeltir. Maliyeti dakikada
        /// bir fazladan tam mesajdır — ihmal edilebilir.
        /// </summary>
        private static readonly TimeSpan ResyncInterval = TimeSpan.FromSeconds(60);

        private readonly ConcurrentDictionary<string, GroupState> _states = new();

        public PriceFeedMessage? BuildNextMessage(string groupName, List<PriceItemDto> currentItems, string timestamp)
        {
            var state = _states.GetOrAdd(groupName, _ => new GroupState());

            // Aynı grup için yayın tek bir tick döngüsünden geliyor, ancak bağlantı anında
            // GetCurrentSnapshot da okuyor. Durum güncellemesini kilit altında yapıyoruz.
            lock (state.Gate)
            {
                var now = DateTime.UtcNow;
                var needsSnapshot =
                    state.LastItems == null ||
                    now - state.LastSnapshotAt >= ResyncInterval ||
                    HasConfigurationChanged(state.LastItems, currentItems);

                if (needsSnapshot)
                {
                    state.Sequence++;
                    state.LastItems = ToIndex(currentItems);
                    state.LastSnapshotItems = currentItems;
                    state.LastSnapshotAt = now;
                    state.LastTimestamp = timestamp;

                    return new PriceFeedMessage
                    {
                        Snapshot = new PriceSnapshotDto
                        {
                            Feed = groupName,
                            Timestamp = timestamp,
                            Sequence = state.Sequence,
                            Items = currentItems
                        }
                    };
                }

                // Yapılandırma aynı → yalnızca fiyatı oynayanları topla.
                List<PriceChangeDto>? changed = null;

                foreach (var item in currentItems)
                {
                    if (!state.LastItems!.TryGetValue(item.Id, out var previous)) continue;
                    if (previous.Bid == item.Bid && previous.Ask == item.Ask) continue;

                    changed ??= new List<PriceChangeDto>();
                    changed.Add(new PriceChangeDto { Id = item.Id, Bid = item.Bid, Ask = item.Ask });
                }

                // Hiçbir fiyat oynamadıysa mesaj yok. Bu, piyasa sakinken ağ trafiğini
                // tamamen sıfırlar.
                if (changed == null) return null;

                state.Sequence++;
                state.LastItems = ToIndex(currentItems);
                state.LastSnapshotItems = currentItems;
                state.LastTimestamp = timestamp;

                return new PriceFeedMessage
                {
                    Delta = new PriceDeltaDto
                    {
                        Feed = groupName,
                        Timestamp = timestamp,
                        Sequence = state.Sequence,
                        Changed = changed
                    }
                };
            }
        }

        public PriceSnapshotDto? GetCurrentSnapshot(string groupName)
        {
            if (!_states.TryGetValue(groupName, out var state)) return null;

            lock (state.Gate)
            {
                if (state.LastSnapshotItems == null) return null;

                // Sıra numarası İLERLETİLMEZ: bu, grubun mevcut durumunun bir kopyasıdır.
                // İstemci bu snapshot'ı aldıktan sonra gelen delta'lar (seq > bu) uygulanır;
                // daha eski numaralı bir mesaj gelirse istemci onu yok saymalıdır.
                return new PriceSnapshotDto
                {
                    Feed = groupName,
                    Timestamp = state.LastTimestamp ?? DateTime.UtcNow.ToString("O"),
                    Sequence = state.Sequence,
                    Items = state.LastSnapshotItems
                };
            }
        }

        public void Forget(string groupName) => _states.TryRemove(groupName, out _);

        /// <summary>
        /// Fiyat dışındaki her şeyin aynı kalıp kalmadığını tek geçişte kontrol eder:
        /// ürün sayısı, kimlikler, ad, kaynak sembol, kategori ve sıra.
        /// Bunlardan biri değiştiyse delta yetmez, tam liste gerekir.
        /// </summary>
        private static bool HasConfigurationChanged(
            Dictionary<string, ItemSnapshot> previous,
            List<PriceItemDto> current)
        {
            if (previous.Count != current.Count) return true;

            foreach (var item in current)
            {
                if (!previous.TryGetValue(item.Id, out var prev)) return true;

                if (prev.Symbol != item.Symbol ||
                    prev.SourceKey != item.SourceKey ||
                    prev.CategoryIndex != item.CategoryIndex ||
                    prev.Category != item.Category ||
                    prev.OrderIndex != item.OrderIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<string, ItemSnapshot> ToIndex(List<PriceItemDto> items)
        {
            var index = new Dictionary<string, ItemSnapshot>(items.Count);

            foreach (var item in items)
            {
                index[item.Id] = new ItemSnapshot(
                    item.Symbol, item.SourceKey, item.Category, item.CategoryIndex, item.OrderIndex,
                    item.Bid, item.Ask);
            }

            return index;
        }

        private sealed class GroupState
        {
            public readonly object Gate = new();

            /// <summary>Karşılaştırma için indekslenmiş son durum.</summary>
            public Dictionary<string, ItemSnapshot>? LastItems;

            /// <summary>Yeni bağlanana olduğu gibi gönderilebilecek son tam liste.</summary>
            public List<PriceItemDto>? LastSnapshotItems;

            public DateTime LastSnapshotAt = DateTime.MinValue;
            public string? LastTimestamp;
            public long Sequence;
        }

        private readonly record struct ItemSnapshot(
            string Symbol,
            string SourceKey,
            string? Category,
            int CategoryIndex,
            int OrderIndex,
            decimal? Bid,
            decimal? Ask);
    }
}
