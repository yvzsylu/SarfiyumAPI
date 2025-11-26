using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class CreateTenantWithUserDto
    {
        // --- Tenant Bilgileri ---
        public string Name { get; set; } = default!;
        public string TaxNumber { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string? Website { get; set; }

        // Adres
        public string City { get; set; } = default!;
        public string District { get; set; } = default!;
        public string? ZipCode { get; set; }
        public string? FullAddress { get; set; }

        // --- Yetkili Kullanıcı Bilgileri ---
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }   
}
