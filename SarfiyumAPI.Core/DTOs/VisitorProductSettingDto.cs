using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class VisitorProductSettingDto
    {
        public string VisitorId { get; set; } = default!;
        public string TenantProductId { get; set; } = default!;
        public decimal? SpecialBuyMultiplier { get; set; }
        public decimal? SpecialSellMultiplier { get; set; }
        public decimal? SpecialAddonAmount { get; set; }
    }
}
