using SarfiyumAPI.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class UpdateSubscriptionDto
    {
        public string Id { get; set; } = default!;  // History ID
        public SubscriptionPlanType PlanType { get; set; } // Plan değişirse süre de güncellenmeli
        public SubscriptionStatus Status { get; set; } // 🔥 EKLENDİ (Dropdown'dan seçilecek)
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public string? Note { get; set; }
    }
}
