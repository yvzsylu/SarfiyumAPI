using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class SymbolDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!; // "ALTIN"
        public string CategoryName { get; set; } = default!; // "MADEN"
        public string CategoryId { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
