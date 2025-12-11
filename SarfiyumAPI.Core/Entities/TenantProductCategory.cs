using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class TenantProductCategory : EntityBase
    {
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // "Maden", "Bilezik"

        public int OrderIndex { get; set; } = 0; // Kategori sırası

        public bool IsActive { get; set; } = true;

        // İlişki: Bir kategori altında birden çok ürün olabilir
        public virtual ICollection<TenantProduct>? TenantProducts { get; set; }
    }
}
