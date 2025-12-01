using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class CreateUpdateSymbolDto
    {
        public string Name { get; set; } = default!;
        public string CategoryId { get; set; } = default!; // Mevcut bir kategori seçilmeli
        public bool IsActive { get; set; } = true;
    }
}
