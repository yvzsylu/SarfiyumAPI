using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class ProductOrderRequestDto
    {
        public string ProductId { get; set; } = default!;  // Hangi ürün?
        public int NewIndex { get; set; }     // Yeni sırası kaç?
    }
}
