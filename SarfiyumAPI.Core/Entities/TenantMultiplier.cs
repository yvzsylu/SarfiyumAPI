using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class TenantMultiplier: EntityBase
    {
        // Hangi Kuyumcu? (Senin User/Tenant tablondaki ID string ise string, int ise int olmalı. 
        // EntityBase string olduğu için burada string varsaydım.)
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string TenantId { get; set; } = string.Empty;

        // Hangi Sembol? (Foreign Key -> SymbolDefinitions)
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string SymbolId { get; set; } = string.Empty;

        [ForeignKey("SymbolId")]
        public virtual Symbol? Symbol { get; set; }

        // --- ÇARPAN AYARLARI ---

        // Alış Çarpanı (Örn: 1.05)
        [Column(TypeName = "decimal(18, 4)")]
        public decimal BuyMultiplier { get; set; } = 1.0000m;

        // Satış Çarpanı (Örn: 1.05)
        [Column(TypeName = "decimal(18, 4)")]
        public decimal SellMultiplier { get; set; } = 1.0000m;

        // Sabit Makas (Örn: +10 TL ekle)
        [Column(TypeName = "decimal(18, 4)")]
        public decimal AddonAmount { get; set; } = 0m;

        // Tenant bu sembolü panelinde görmek istemeyebilir
        public bool IsActive { get; set; } = true;
    }
}
