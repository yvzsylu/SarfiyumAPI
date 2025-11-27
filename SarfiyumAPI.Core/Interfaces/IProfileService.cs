using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface IProfileService
    {
        Task<ServiceResult<UserProfileDto>> GetProfileInfoAsync(string userId);
    }
}
