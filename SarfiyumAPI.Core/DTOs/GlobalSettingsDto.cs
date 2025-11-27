using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class GlobalSettingsDto
    {
        public SmtpSettingsDto Smtp { get; set; } = default!;
        // public GeneralSettingsDto General { get; set; } // İleride eklersin
    }
}
