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

        Task<ServiceResult<List<UserListDto>>> GetAllUsersAsync();

        // 2. Bir kullanıcının mevcut rollerini getir (Frontend'deki checkbox'ları işaretlemek için)
        Task<ServiceResult<List<string>>> GetUserRolesAsync(string userId);

        // 3. Kullanıcıya rolleri ata/güncelle (Mevcut rolleri silip yenilerini ekler)
        // Kullanılacak Identity metotları: GetRolesAsync, RemoveFromRolesAsync, AddToRolesAsync
        Task<ServiceResult<bool>> UpdateUserRolesAsync(string userId, List<string> roleNames);

        Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive);

        Task<ServiceResult<string>> UpdateUserAsync(UserUpdateDto model);

        Task<ServiceResult<UserListDto>> GetUserByIdAsync(string id);
    }
}
