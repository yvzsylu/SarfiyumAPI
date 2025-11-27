using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UserUpdateDto
    {

        public string Id { get; set; } = default!;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public string RoleId { get; set; } = string.Empty;

        public string? TenantId { get; set; }
        public bool IsActive { get; set; } = default!;
    }
}
