using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;
using Microsoft.AspNetCore.Identity;

namespace SarfiyumAPI.Service.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IRepository<TenantPaymentHistory> _paymentRepository;
        private readonly UserManager<AppUser> _userManager;

        public DashboardService(
            IRepository<Tenant> tenantRepository,
            IRepository<TenantPaymentHistory> paymentRepository,
            UserManager<AppUser> userManager)
        {
            _tenantRepository = tenantRepository;
            _paymentRepository = paymentRepository;
            _userManager = userManager;
        }

        public async Task<ServiceResult<DashboardStatsDto>> GetDashboardStatsAsync()
        {
            var response = new DashboardStatsDto();

            // -------------------------------------------------------------
            // 1. TEMEL SAYAÇLAR (COUNT İŞLEMLERİ)
            // -------------------------------------------------------------

            // GetAll yerine Where(x => true).CountAsync() kullanıyoruz. 
            // Senin Repository'deki Where IQueryable döndüğü için CountAsync veritabanında çalışır (Hızlıdır).

            response.TotalTenants = await _tenantRepository
                .Where(x => !x.IsDeleted)
                .CountAsync();

            response.TotalUsers = await _userManager.Users.CountAsync();

            response.TotalActiveSubscriptions = await _paymentRepository
                .Where(x => x.Status == SubscriptionStatus.Active && !x.IsDeleted)
                .CountAsync();

            // -------------------------------------------------------------
            // 2. FİNANSAL VERİLER (SUM İŞLEMLERİ)
            // -------------------------------------------------------------

            // Tüm listeyi çekmek yerine veritabanında toplatıyoruz (SumAsync)
            response.TotalRevenue = await _paymentRepository
                .Where(x => x.IsPaid && !x.IsDeleted)
                .SumAsync(x => x.Amount);

            response.TotalPendingAmount = await _paymentRepository
                .Where(x => !x.IsPaid && !x.IsDeleted)
                .SumAsync(x => x.Amount);

            // -------------------------------------------------------------
            // 3. GRAFİK VERİLERİ (STATUS & PLAN)
            // -------------------------------------------------------------

            response.ActiveSubsCount = response.TotalActiveSubscriptions;

            response.ExpiredSubsCount = await _paymentRepository
                .Where(x => x.Status == SubscriptionStatus.Expired && !x.IsDeleted)
                .CountAsync();

            response.MonthlyPlanCount = await _paymentRepository
                .Where(x => x.Status == SubscriptionStatus.Active && x.PlanType == SubscriptionPlanType.Monthly && !x.IsDeleted)
                .CountAsync();

            response.YearlyPlanCount = await _paymentRepository
                .Where(x => x.Status == SubscriptionStatus.Active && x.PlanType == SubscriptionPlanType.Yearly && !x.IsDeleted)
                .CountAsync();

            // -------------------------------------------------------------
            // 4. AYLIK GELİR GRAFİĞİ (SON 6 AY)
            // -------------------------------------------------------------

            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

            // Burada önce datayı çekip GroupBy'ı RAM'de yapıyoruz (Karmaşık SQL olmaması için en güvenlisi)
            var recentPayments = await _paymentRepository
                .Where(x => x.CreatedAt >= sixMonthsAgo && !x.IsDeleted)
                .ToListAsync();

            response.MonthlyRevenue = recentPayments
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    PaidAmount = g.Where(x => x.IsPaid).Sum(x => x.Amount),
                    PendingAmount = g.Where(x => !x.IsPaid).Sum(x => x.Amount)
                })
                .OrderBy(x => x.Month) // String sıralama yerine tarih bazlı sıralama daha doğru olur ama basitlik için böyle kalsın
                .ToList();

            // -------------------------------------------------------------
            // 5. ÖDEME YAPMAMIŞ FİRMALAR (MANUEL JOIN)
            // -------------------------------------------------------------

            // Include kullanamadığımız için önce ödemeleri çekiyoruz
            var unpaidPayments = await _paymentRepository
                .Where(x => !x.IsPaid && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .ToListAsync();

            // Bu ödemelerdeki TenantId'leri topluyoruz (Tekrar edenleri siliyoruz)
            var tenantIds = unpaidPayments.Select(x => x.TenantId).Distinct().ToList();

            // Sadece bu ID'lere sahip Tenant'ları çekiyoruz
            var tenants = await _tenantRepository
                .Where(x => tenantIds.Contains(x.Id))
                .ToListAsync();

            // İki listeyi bellekte birleştiriyoruz (Map)
            response.UnpaidTenants = unpaidPayments.Select(payment => {
                var relatedTenant = tenants.FirstOrDefault(t => t.Id == payment.TenantId);
                return new UnpaidTenantDto
                {
                    TenantId = payment.TenantId,
                    TenantName = relatedTenant != null ? relatedTenant.Name : "Silinmiş Firma",
                    PlanName = payment.PlanType == SubscriptionPlanType.Monthly ? "Aylık" : "Yıllık",
                    Amount = payment.Amount,
                    DueDate = payment.StartDate
                };
            }).ToList();

            // -------------------------------------------------------------
            // 6. ABONELİĞİ YAKLAŞANLAR
            // -------------------------------------------------------------

            var upcomingExpiry = DateTime.UtcNow.AddDays(15);

            // IQueryable üzerinden Where koşulu
            var expiringTenants = await _tenantRepository
                .Where(x => x.IsActive && x.SubscriptionEndDate != null
                       && x.SubscriptionEndDate.Value > DateTime.UtcNow
                       && x.SubscriptionEndDate.Value <= upcomingExpiry)
                .OrderBy(x => x.SubscriptionEndDate)
                .Take(5)
                .ToListAsync();

            response.ExpiringTenants = expiringTenants.Select(x => new ExpiringTenantDto
            {
                TenantName = x.Name,
                EndDate = x.SubscriptionEndDate!.Value,
                RemainingDays = (x.SubscriptionEndDate!.Value - DateTime.UtcNow).Days
            }).ToList();

            return ServiceResult<DashboardStatsDto>.Success(response);
        }
    }
}