using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface IAuthService
    {
        // Dönüş tipleri ServiceResult<T> ile sarıldı
        Task<ServiceResult<string>> LoginAsync(LoginDto model); // Token string döner
        Task<ServiceResult<string>> ResetPasswordAsync(string email);

    }
}
