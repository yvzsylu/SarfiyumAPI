using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class SystemCatalog : EntityBase
    {
        [Required]
        [StringLength(50)]
        public string SourceKey { get; set; } = default!; // Örn: "ALTIN" (Socket'teki Symbol)

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = default!; // Örn: "Has Altın" (Ekranda görünecek)
    }
}
