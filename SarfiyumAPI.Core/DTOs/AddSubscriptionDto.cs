using SarfiyumAPI.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    // Abonelik Ekleme İsteği İçin
    public class AddSubscriptionDto
    {
        public string TenantId { get; set; } = default!;
        public SubscriptionPlanType PlanType { get; set; } // Monthly / Yearly
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; } = true;
        public string? Note { get; set; }
    }
}
