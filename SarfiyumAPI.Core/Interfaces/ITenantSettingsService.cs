using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ITenantSettingsService
    {

        Task<ServiceResult<TenantSettingsDto>> GetSettingsAsync(string tenantId);
        Task<ServiceResult<string>> UpdateSettingsAsync(string tenantId, UpdateTenantSettingsDto dto);
        Task<ServiceResult<ThemeSettingsDto>> GetThemeSettingsAsync(string tenantId);
        Task<ServiceResult<string>> UpdateThemeSettingsAsync(string tenantId, UpdateThemeSettingsDto dto);
    }
}
