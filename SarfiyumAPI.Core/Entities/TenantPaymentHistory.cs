using SarfiyumAPI.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class TenantPaymentHistory : EntityBase
    {
        [Required]
        [StringLength(EntityConstants.KeyLength)]
        public string TenantId { get; set; } = string.Empty;

        // Aylık mı Yıllık mı?
        public SubscriptionPlanType PlanType { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        // Ne kadar ödedi?
        public decimal Amount { get; set; }

        // Ödeme alındı mı? (Admin panelinden "Ödenmedi" diye işaretleyip sonra düzeltebilirsin)
        public bool IsPaid { get; set; }

        // Bu ödeme hangi tarihte başladı?
        public DateTime StartDate { get; set; }

        // Bu ödeme hangi tarihe kadar uzattı?
        public DateTime EndDate { get; set; }

        // Admin notu (Örn: "EFT ile gönderdi", "Elden alındı" vb.)
        public string? Note { get; set; }
    }
}
