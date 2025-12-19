using Coravel.Invocable;
using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;

namespace SarfiyumAPI.Service.Jobs
{
    public class ExpirationJob : IInvocable
    {
        private readonly IRepository<TenantPaymentHistory> _paymentRepository;
        private readonly IDataContext _context;

        // Coravel Dependency Injection'ı otomatik halleder.
        // Repository ve Context'i constructor'da istiyoruz.
        public ExpirationJob(IRepository<TenantPaymentHistory> paymentRepository, IDataContext context)
        {
            _paymentRepository = paymentRepository;
            _context = context;
        }

        public async Task Invoke()
        {
            // UTC zamanına göre şu andan önce bitmiş ve hala 'Active' olanları bul
            var expiredSubs = await _paymentRepository
                .Where(x => x.Status == SubscriptionStatus.Active && x.EndDate < DateTime.UtcNow)
                .ToListAsync();

            if (expiredSubs.Any())
            {
                foreach (var sub in expiredSubs)
                {
                    sub.Status = SubscriptionStatus.Expired;
                    _paymentRepository.Update(sub);
                }

                // Değişiklikleri kaydet
                await _context.SaveChangesAsync();

                // Buraya istersen ILogger ekleyip log atabilirsin
                Console.WriteLine($"[Job Çalıştı] {expiredSubs.Count} adet abonelik 'Expired' statüsüne çekildi.");
            }
        }
    }
}
