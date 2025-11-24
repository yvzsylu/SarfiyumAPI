using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface IAccountService
    {
        Task<ServiceResult<string>> CreateUserAsync(CreateUserDto model);

        Task<ServiceResult<string>> AssignRoleAsync(UserRoleDto model);
    }
}
