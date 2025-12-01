using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class TenantSettingResponseDto
    {
        public string SymbolId { get; set; } = default!;
        public string SymbolName { get; set; } = default!;      // "ALTIN"
        public string CategoryName { get; set; } = default!;    // "MADEN"

        // Tenant'ın Ayarları (Yoksa default döneceğiz)
        public decimal BuyMultiplier { get; set; }
        public decimal SellMultiplier { get; set; }
        public decimal AddonAmount { get; set; }
    }
}
