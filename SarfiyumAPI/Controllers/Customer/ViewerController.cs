using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Service.Services;

namespace SarfiyumAPI.Controllers.User
{
    [Route("api/user/[controller]")]
    [ApiController]
    [Authorize(Roles = "User,Admin")] // Sadece User(Tenant Sahibi) ve Admin erişebilir
    public class ViewerController : CustomBaseController
    {
        private readonly IViewerAccountService _viewerService;

        public ViewerController(IViewerAccountService viewerService)
        {
            _viewerService = viewerService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetViewers()
        {
            return CreateActionResult(await _viewerService.GetMyViewersAsync());
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateViewer(CreateUserDto model)
        {
            // DTO'daki TenantId ve RoleId frontend'den gelse bile Service bunları ezecek.
            return CreateActionResult(await _viewerService.CreateViewerAsync(model));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return CreateActionResult(await _viewerService.GetViewerByIdAsync(id));
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateViewer(UserUpdateDto model)
        {
            return CreateActionResult(await _viewerService.UpdateViewerAsync(model));
        }

        [HttpPatch("status/{id}")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] bool isActive)
        {
            return CreateActionResult(await _viewerService.UpdateStatusAsync(id, isActive));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteViewer(string id)
        {
            return CreateActionResult(await _viewerService.DeleteViewerAsync(id));
        }
    }
}