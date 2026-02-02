using System.ComponentModel.DataAnnotations.Schema;

namespace SarfiyumAPI.Core.Entities
{
    public class VisitorProductSetting : EntityBase
    {
        public string TenantId { get; set; } = default!;

        // 1. Sadece ID ile bağlamak yeterli (Foreign Key)
        public string VisitorId { get; set; } = default!;

        // 2. Navigation Property (Okurken 'Include' yapabilmek için lazım)
        // 🔥 new() ASLA YAPMA! Nullable (?) yap.
        [ForeignKey("VisitorId")]
        public virtual AppUser? Visitor { get; set; }

        public string TenantProductId { get; set; } = default!;

        // 🔥 new() ASLA YAPMA! Nullable (?) yap.
        [ForeignKey("TenantProductId")]
        public virtual TenantProduct? TenantProduct { get; set; }

        [Column(TypeName = "decimal(18, 10)")]
        public decimal? SpecialBuyMultiplier { get; set; }

        [Column(TypeName = "decimal(18, 10)")]
        public decimal? SpecialSellMultiplier { get; set; }

        [Column(TypeName = "decimal(18, 10)")]
        public decimal? SpecialAddonAmount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}