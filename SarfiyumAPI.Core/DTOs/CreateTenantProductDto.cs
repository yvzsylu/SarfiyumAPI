using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class CreateTenantProductDto
    {
        public string Name { get; set; } = default!; // Örn: "22 Ayar Halhal"
        public string CategoryId { get; set; } = default!; // Hangi kategoriye eklenecek?
        public string SourceKey { get; set; } = "ALTIN"; // Neye endeksli? (Default ALTIN)

        public decimal BuyMultiplier { get; set; } = 1.0m; // Örn: 0.916
        public decimal SellMultiplier { get; set; } = 1.0m; // Örn: 0.930
        public decimal AddonAmount { get; set; } = 0m;

        public int OrderIndex { get; set; } = 99; // Sıralama

        public bool ShowOnWeb { get; set; } = true;
        public bool ShowOnMobile { get; set; } = true;


    }
}
