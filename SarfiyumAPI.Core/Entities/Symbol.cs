using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class Symbol : EntityBase
    {
        // Socket.IO'dan gelen ham kod (Örn: "ALTIN", "USDTRY")
// Örn: "ALTIN", "USDTRY"
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        // --- İLİŞKİLER ---
        
        // 1. Kategori İlişkisi (Zorunlu)
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string SymbolCategoryId { get; set; } = string.Empty;

        [ForeignKey("SymbolCategoryId")]
        public virtual SymbolCategory? Category { get; set; }

        // 2. Tenant Ayarları İlişkisi (EKLENEN KISIM)
        // Bir sembolün, binlerce tenant'ta karşılığı (çarpan ayarı) olabilir.
        public virtual ICollection<TenantMultiplier>? TenantMultipliers { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
