using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    [Route("api/admin/dashboard")]
    [ApiController]
    // [Authorize(Roles = "Admin")] // Admin rolü kontrolünü açmalısın
    [Authorize(Roles = "Admin")]
    public class DashboardController : CustomBaseController
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            return CreateActionResult(await _dashboardService.GetDashboardStatsAsync());
        }
    }
}