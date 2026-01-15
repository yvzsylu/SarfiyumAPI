using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class TenantSetting : EntityBase
    {
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string TenantId { get; set; } = string.Empty;

        // --- AYARLAR ---
        public string? WhatsAppNumber { get; set; }

        // İleride eklenebilecek örnekler (Şimdilik yorum satırı):
        // public string? ThemeColor { get; set; }
        // public bool ShowLogoInHeader { get; set; }
    }
}
