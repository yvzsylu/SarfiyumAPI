using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface IRoleService
    {
        Task<ServiceResult<List<RoleDto>>> GetAllRolesAsync();
        Task<ServiceResult<string>> CreateRoleAsync(CreateRoleDto roleName);
        Task<ServiceResult<string>> UpdateRoleAsync(RoleDto model);
        Task<ServiceResult<string>> DeleteRoleAsync(string id); // YENİ
    }
}
