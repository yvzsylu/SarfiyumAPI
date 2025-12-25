using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public interface IViewerAccountService
    {
        Task<ServiceResult<List<UserListDto>>> GetMyViewersAsync();
        Task<ServiceResult<string>> CreateViewerAsync(CreateUserDto model);
        Task<ServiceResult<UserListDto>> GetViewerByIdAsync(string id);
        Task<ServiceResult<string>> UpdateViewerAsync(UserUpdateDto model);
        Task<ServiceResult<string>> UpdateStatusAsync(string id, bool isActive);
        Task<ServiceResult<string>> DeleteViewerAsync(string id);
    }
}