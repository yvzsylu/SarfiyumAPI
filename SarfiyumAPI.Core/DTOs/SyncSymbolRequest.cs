using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class SyncSymbolRequest
    {
        public string Symbol { get; set; } = default!; // "ALTIN"
        public string Category { get; set; } = default!;
        public string Description { get; set; } = default!;
    }
}
