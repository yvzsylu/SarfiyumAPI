using SarfiyumAPI.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class UserActiveSession : EntityBase
    {
        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        public string TenantId { get; set; } = default!;

        [Required]
        public ClientSource ClientSource { get; set; } // Web mi Mobile mi?

        [Required]
        public string SessionId { get; set; } = default!; // Token içindeki benzersiz ID

        public DateTime LastLoginDate { get; set; } = DateTime.UtcNow;

        // Opsiyonel: SignalR ConnectionId (Anlık kicklemek istersen)
        public string? ConnectionId { get; set; }
    }
}
