using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SarfiyumAPI.Core.Entities
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public string? TenantId { get; set; } = default!;
        public Tenant? Tenant { get; set; } = default!;


    }
}
