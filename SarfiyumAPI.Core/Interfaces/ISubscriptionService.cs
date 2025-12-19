using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Wrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ISubscriptionService
    {
        // Bir firmaya abonelik ekler (Süreyi uzatır ve geçmişe kaydeder)
        Task<ServiceResult<string>> AddSubscriptionAsync(AddSubscriptionDto model);

        // Bir firmanın ödeme/abonelik geçmişini getirir
        Task<ServiceResult<List<PaymentHistoryDto>>> GetHistoryByTenantIdAsync(string tenantId);

        Task<ServiceResult<PaymentHistoryDto>> GetSubscriptionByIdAsync(string id);

        Task<ServiceResult<string>> UpdateSubscriptionAsync(UpdateSubscriptionDto model);

        Task<ServiceResult<string>> DeleteSubscriptionAsync(string historyId);

        Task<ServiceResult<string>> AdjustDaysAsync(AdjustSubscriptionDaysDto model);
    }
}
