using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class UserLoginHistory : EntityBase
    {
        [Required]
        public string UserId { get; set; } = default!;

        public string? UserName { get; set; } // Hızlı raporlama için denormalize edebiliriz

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; } // Tarayıcı ve Cihaz bilgisi (Chrome, iPhone vs.)

        public DateTime LoginDate { get; set; } = DateTime.UtcNow;

        public bool IsSuccess { get; set; } = true; // Başarılı/Başarısız ayrımı
    }
}
