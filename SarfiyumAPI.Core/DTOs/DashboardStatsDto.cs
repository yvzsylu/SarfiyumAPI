using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class DashboardStatsDto
    {
        // Kartlar için
        public int TotalTenants { get; set; }
        public int TotalUsers { get; set; }
        public int TotalActiveSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }     // Toplam Tahsilat
        public decimal TotalPendingAmount { get; set; } // Bekleyen Ödeme

        // Grafikler için
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = [];   // Performance Chart
        public int ActiveSubsCount { get; set; }
        public int ExpiredSubsCount { get; set; }
        public int MonthlyPlanCount { get; set; }
        public int YearlyPlanCount { get; set; }

        // Tablolar için
        public List<UnpaidTenantDto> UnpaidTenants { get; set; } = [];  // Ödeme Yapmamışlar
        public List<ExpiringTenantDto> ExpiringTenants { get; set; } = []; // Süresi Bitenler
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = default!;
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
    }

    public class UnpaidTenantDto
    {
        public string TenantId { get; set; } = default!;
        public string TenantName { get; set; } = default!;
        public string PlanName { get; set; } = default!;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class ExpiringTenantDto
    {
        public string TenantName { get; set; } = default!;
        public DateTime EndDate { get; set; }
        public int RemainingDays { get; set; }
    }
}
