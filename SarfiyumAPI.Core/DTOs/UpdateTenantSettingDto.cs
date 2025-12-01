using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UpdateTenantSettingDto
    {
        public string SymbolId { get; set; } = default!;
        public decimal BuyMultiplier { get; set; }
        public decimal SellMultiplier { get; set; }
        public decimal AddonAmount { get; set; }
    }
}
