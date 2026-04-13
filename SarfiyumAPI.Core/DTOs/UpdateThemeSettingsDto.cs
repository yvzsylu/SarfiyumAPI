using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UpdateThemeSettingsDto
    {
        public string? DashboardTheme { get; set; }
        public string? UiSettingsJson { get; set; }
        public string? CustomColorsJson { get; set; }
    }
}
