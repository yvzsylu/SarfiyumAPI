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
    public interface ITenantSettingsService
    {
        Task<Dictionary<string, TenantMultiplier>> GetMultipliersAsync(string tenantId);
        void ClearCache(string tenantId);

        Task<ServiceResult<List<TenantSettingResponseDto>>> GetSettingsForUIAsync(string tenantId);
        Task<ServiceResult<string>> UpdateSettingsAsync(string tenantId, List<UpdateTenantSettingDto> updates);
    }
}
