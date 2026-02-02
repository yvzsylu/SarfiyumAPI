using SarfiyumAPI.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class VisitorSessionDto
    {
        public string VisitorId { get; set; } = default!;
        public List<VisitorProductSetting> Settings { get; set; } = new();
    }
}
