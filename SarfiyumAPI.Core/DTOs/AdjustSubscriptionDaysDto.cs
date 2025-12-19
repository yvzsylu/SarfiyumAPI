using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class AdjustSubscriptionDaysDto
    {
        public string Id { get; set; } // 🔥 Değişti: Artık TenantId değil, Subscription (History) ID
        public int Days { get; set; } // +5 veya -2
    }
}
