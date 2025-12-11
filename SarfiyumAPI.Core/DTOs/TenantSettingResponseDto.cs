using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class TenantSettingResponseDto
    {
        public string Id { get; set; } = default!; // Product ID (Guid)
        public string Name { get; set; } = default!; // Görünen İsim
        public string SourceKey { get; set; } = default!;// Kaynak (ALTIN, USDTRY)
        public string CategoryName { get; set; } = default!;
        public string CategoryId { get; set; } = default!;

        public decimal BuyMultiplier { get; set; }
        public decimal SellMultiplier { get; set; }
        public decimal AddonAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
