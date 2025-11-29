using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class SymbolCategory : EntityBase
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // Örn: "MADEN", "DÖVİZ"

        // İlişki: Bir kategorinin birden çok sembolü olabilir
        public virtual ICollection<Symbol>? Symbols { get; set; }
    }
}
