using SarfiyumAPI.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    // Geçmişi Listelemek İçin
    public class PaymentHistoryDto
    {
        public string Id { get; set; } = default!;
        public SubscriptionStatus Status { get; set; }
        public string PlanName { get; set; } = default!; // "Aylık" veya "Yıllık"
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedDate { get; set; } // İşlem Tarihi
        public string? Note { get; set; }
    }
}
