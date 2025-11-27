using Microsoft.AspNetCore.Http;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ITenantService
    {
        Task<ServiceResult<List<TenantDto>>> GetAllAsync();
        Task<ServiceResult<TenantDto>> GetByIdAsync(string id);
        Task<ServiceResult<string>> CreateAsync(TenantDto model);
        Task<ServiceResult<string>> UpdateAsync(TenantDto model);
        Task<ServiceResult<string>> DeleteAsync(string id);

        Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive);
        // IFormFile kullanabilmek için: using Microsoft.AspNetCore.Http;
        Task<ServiceResult<string>> UploadLogoAsync(string tenantId, IFormFile file);

    }
}
