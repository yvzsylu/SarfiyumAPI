using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UserProfileDto
    {
        // --- KULLANICI BİLGİLERİ ---
        public string Id { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? PhoneNumber { get; set; } // IdentityUser'dan gelir
        public bool IsUserActive { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();

        // --- TENANT (FİRMA) BİLGİLERİ ---
        // Eğer kullanıcı bir firmaya bağlı değilse (Admin) bu alanlar null olabilir.
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? TenantTaxNumber { get; set; }
        public string? TenantPhoneNumber { get; set; }
        public string? TenantWebsite { get; set; }
        public string? TenantLogoUrl { get; set; }
        public bool IsTenantActive { get; set; }

        // --- TENANT ADRES BİLGİLERİ ---
        public string? TenantCity { get; set; }
        public string? TenantDistrict { get; set; }
        public string? TenantCountry { get; set; }
        public string? TenantZipCode { get; set; }
        public string? TenantFullAddress { get; set; }

        // Ekstra: Kayıt Tarihleri
        public DateTime? TenantCreatedAt { get; set; }
    }
}
