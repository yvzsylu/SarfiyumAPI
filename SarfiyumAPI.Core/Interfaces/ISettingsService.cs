using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ISettingsService
    {
        // Tüm ayarları getir (Frontend'de sayfa açılınca doldurmak için)
        Task<ServiceResult<GlobalSettingsDto>> GetAllSettingsAsync();

        // Sadece SMTP kısmını güncelle
        Task<ServiceResult<string>> UpdateSmtpSettingsAsync(SmtpSettingsDto model);

        // Internal: Diğer servislerin (EmailService) ham veriye erişmesi için
        Task<AppSetting> GetConfigInternalAsync();
    }
}
