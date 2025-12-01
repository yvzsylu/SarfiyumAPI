using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ISymbolService
    {
        Task<ServiceResult<string>> SyncManualAsync(List<SyncSymbolRequest> requestData);

        Task<ServiceResult<List<SymbolDto>>> GetAllAsync();
        Task<ServiceResult<string>> CreateAsync(CreateUpdateSymbolDto model);
        Task<ServiceResult<string>> UpdateAsync(string id, CreateUpdateSymbolDto model);
        Task<ServiceResult<string>> DeleteAsync(string id);
        Task<ServiceResult<string>> ToggleStatusAsync(string id);
        Task<ServiceResult<SymbolDto>> GetByIdAsync(string id);
    }
}
