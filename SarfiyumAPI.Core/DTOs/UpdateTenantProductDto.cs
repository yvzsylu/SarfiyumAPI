using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UpdateTenantProductDto
    {
        public string Id { get; set; } = default!; // Product GUID
        public string Name { get; set; } = default!; // İsmi de değiştirebilsin

        public string CategoryId { get; set; } = default!; // İsmi de değiştirebilsin

        public decimal BuyMultiplier { get; set; }
        public decimal SellMultiplier { get; set; }

        public decimal AddonAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
