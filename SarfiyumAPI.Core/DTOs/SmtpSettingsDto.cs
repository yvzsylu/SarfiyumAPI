using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class SmtpSettingsDto
    {
        public string Host { get; set; } = default!;
        public int Port { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public bool EnableSsl { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
    }
}
