using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ISymbolCategoryService
    {
        Task<ServiceResult<List<SymbolCategoryDto>>> GetAllAsync();
        Task<ServiceResult<SymbolCategoryDto>> GetByIdAsync(string id);
        Task<ServiceResult<string>> CreateAsync(CreateUpdateSymbolCategoryDto model);
        Task<ServiceResult<string>> UpdateAsync(string id, CreateUpdateSymbolCategoryDto model);
        Task<ServiceResult<string>> DeleteAsync(string id);
    }
}
