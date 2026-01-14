using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class TenantProduct : EntityBase
    {
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string TenantId { get; set; } = string.Empty;

        // --- İLİŞKİLER ---
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string TenantProductCategoryId { get; set; } = string.Empty;

        [ForeignKey("TenantProductCategoryId")]
        public virtual TenantProductCategory? TenantProductCategory { get; set; }

        // --- ÜRÜN BİLGİLERİ ---
        [Required]
        public string Name { get; set; } = default!; // Örn: "22 Ayar Ajda"

        [Required]
        public string SourceKey { get; set; } = default!; // Örn: "ALTIN", "USDTRY"

        // --- ÇARPANLAR ---
        [Column(TypeName = "decimal(18, 10)")]
        public decimal BuyMultiplier { get; set; } = 1.0000000000m;

        [Column(TypeName = "decimal(18, 10)")]
        public decimal SellMultiplier { get; set; } = 1.0000000000m;

        [Column(TypeName = "decimal(18, 10)")]
        public decimal AddonAmount { get; set; } = 0m;

        public int OrderIndex { get; set; } // Ürün sırası
        public bool IsActive { get; set; } = true;
        public bool ShowOnWeb { get; set; } = true;    // YENİ
        public bool ShowOnMobile { get; set; } = true; // YENİ
    }
}
