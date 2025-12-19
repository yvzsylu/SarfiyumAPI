using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [Tags("Admin/Subscription")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Sadece Admin erişebilir
    public class SubscriptionController : CustomBaseController
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // POST: api/admin/Subscription
        // Bir firmaya süre ekler
        [HttpPost]
        public async Task<IActionResult> AddSubscription([FromBody] AddSubscriptionDto model)
        {
            return CreateActionResult(await _subscriptionService.AddSubscriptionAsync(model));
        }

        // GET: api/admin/Subscription/history/{tenantId}
        // Bir firmanın geçmişini getirir
        [HttpGet("history/{tenantId}")]
        public async Task<IActionResult> GetHistory(string tenantId)
        {
            return CreateActionResult(await _subscriptionService.GetHistoryByTenantIdAsync(tenantId));
        }

        // ... 

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return CreateActionResult(await _subscriptionService.GetSubscriptionByIdAsync(id));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSubscriptionDto model)
        {
            return CreateActionResult(await _subscriptionService.UpdateSubscriptionAsync(model));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return CreateActionResult(await _subscriptionService.DeleteSubscriptionAsync(id));
        }

        [HttpPost("adjust-days")]
        public async Task<IActionResult> AdjustDays([FromBody] AdjustSubscriptionDaysDto model)
        {
            // Mevcut aboneliğin gününü değiştirir.
            return CreateActionResult(await _subscriptionService.AdjustDaysAsync(model));
        }
    }
}