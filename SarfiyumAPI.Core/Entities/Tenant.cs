using SarfiyumAPI.Core.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class Tenant : EntityBase
    {
        public string Name { get; set; } = default!;
        public string TaxNumber { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true; // Varsayılan Aktif

        // BEST PRACTICE: Adres Bilgisi
        // Veritabanında ayrı bir tablo olmaz, Tenants tablosunda 
        // Address_City, Address_Street gibi kolonlar oluşur.
        public Address Address { get; set; } = new Address();

        // İSİM DEĞİŞİKLİĞİ: Employees -> Users
        public ICollection<AppUser> Users { get; set; }
    }
}
