using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class TenantDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = default!;
        public string TaxNumber { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } 

        // Address (Flattened - Düzleştirilmiş)
        public string City { get; set; } = default!;
        public string District { get; set; } = default!;
        public string? ZipCode { get; set; }
        public string? FullAddress { get; set; }
        public string Country { get; set; } = "Türkiye";
    }
}
