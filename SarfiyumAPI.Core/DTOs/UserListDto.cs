using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UserListDto
    {
        public string Id { get; set; } = default!; // AppUser Identity Id
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Username { get; set; } = default!;

        public string? TenantId { get; set; }
        public string? TenantName { get; set; } // Tenant tablosundan join ile gelecek

        public bool IsActive { get; set; } // AppUser'daki aktiflik durumu
        public List<string> Roles { get; set; } = new List<string>(); // Kullanıcının rolleri
    }
}
