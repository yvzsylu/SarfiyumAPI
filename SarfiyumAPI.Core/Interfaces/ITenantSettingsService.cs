using SarfiyumAPI.Core.Entities;
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
    }
}
