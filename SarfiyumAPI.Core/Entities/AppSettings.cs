using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class AppSetting : EntityBase
    {
        // --- SMTP AYARLARI ---
        public string SmtpHost { get; set; } = default!;
        public int SmtpPort { get; set; }
        public string SmtpEmail { get; set; } = default!;
        public string SmtpPassword { get; set; } = default!;
        public bool SmtpEnableSsl { get; set; }
        public string SmtpDisplayName { get; set; } = "Sarfiyum System";

        // --- İLERİDE EKLENECEK DİĞER AYARLAR (Örnek) ---
        // public string SiteTitle { get; set; }
        // public string LogoUrl { get; set; }
        // public bool IsMaintenanceMode { get; set; }
    }
}
