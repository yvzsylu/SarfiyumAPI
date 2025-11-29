using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class PriceData
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = default!; // "ALTIN"

        [JsonPropertyName("category")]
        public string Category { get; set; } = default!;// "MADEN"

        [JsonPropertyName("bid")]
        public decimal? Bid { get; set; } = default!; // Null gelebilir

        [JsonPropertyName("ask")]
        public decimal? Ask { get; set; } = default!;// Null gelebilir

        [JsonPropertyName("description")]
        public string Description { get; set; } = default!;

        [JsonPropertyName("source")]
        public string? Source { get; set; } = default!;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = default!;// String (0000 yılı hatasını önlemek için)
    }
}
