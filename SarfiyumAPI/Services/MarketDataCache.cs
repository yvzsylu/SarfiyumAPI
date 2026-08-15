using System.Collections.Concurrent;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Services
{
    /// <inheritdoc cref="IMarketDataCache"/>
    public class MarketDataCache : IMarketDataCache
    {
        private readonly ConcurrentDictionary<string, PriceData> _prices = new();

        public void Update(PriceData item)
        {
            if (item?.Symbol == null) return;
            _prices[item.Symbol] = item;
        }

        public bool UpdateRange(IEnumerable<PriceData> items)
        {
            var touched = false;

            foreach (var item in items)
            {
                if (item?.Symbol == null) continue;
                _prices[item.Symbol] = item;
                touched = true;
            }

            return touched;
        }

        public IReadOnlyDictionary<string, PriceData> Snapshot()
            => new Dictionary<string, PriceData>(_prices);
    }
}
