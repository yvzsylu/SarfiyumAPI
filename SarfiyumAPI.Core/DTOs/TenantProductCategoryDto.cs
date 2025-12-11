using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class TenantProductCategoryDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int OrderIndex { get; set; }
    }
}
