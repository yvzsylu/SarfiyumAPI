using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class TenantDto
    {
        public string? Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string TaxNumber { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string? Website { get; set; } = default!;



        // Adres Alanları
        public string City { get; set; } = default!;
        public string District { get; set; } = default!;
        public string Street { get; set; } = default!;
        public string FullAddress { get; set; } = default!;
        public string? ZipCode { get; set; } // EKLENDİ
    }
}
