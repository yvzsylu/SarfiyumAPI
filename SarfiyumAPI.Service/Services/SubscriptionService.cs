using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Enums;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IRepository<TenantPaymentHistory> _paymentRepository;
        private readonly IRepository<Tenant> _tenantRepository;
        private readonly IDataContext _context;
        private readonly ITenantService _tenantService;

        public SubscriptionService(
            IRepository<TenantPaymentHistory> paymentRepository,
            IRepository<Tenant> tenantRepository,
            IDataContext context,
            ITenantService tenantService)
        {
            _paymentRepository = paymentRepository;
            _tenantRepository = tenantRepository;
            _context = context;
            _tenantService = tenantService;
        }

        /// <summary>
        /// Aktif tenant listesi önbelleğini düşürür.
        ///
        /// <c>GetActiveTenantIdsAsync</c> artık <c>SubscriptionEndDate</c>'i de filtreliyor
        /// (aboneliği biten tenant'ın yayını durur). Bu liste 10 dakika önbelleklendiği için,
        /// abonelik değiştiren HER işlem önbelleği düşürmek zorunda — aksi halde aboneliği
        /// yenilenen bir tenant 10 dakikaya kadar yayına dönemez.
        /// </summary>
        private void InvalidateBroadcastTenantList() => _tenantService.ClearTenantCache();

        // -----------------------------------------------------------
        // 1. YENİ ABONELİK EKLEME (Strict Mode & Standart Tipler)
        // -----------------------------------------------------------
        public async Task<ServiceResult<string>> AddSubscriptionAsync(AddSubscriptionDto model)
        {
            var tenant = await _tenantRepository.GetByIdAsync(model.TenantId);
            if (tenant == null) return ServiceResult<string>.Fail("Firma bulunamadı.", 404);

            // --- KURAL: AKTİF ABONELİK VARSA ENGELLE ---
            // Status=Active olan bir kayıt varsa ekleme yapma.
            bool hasActiveSub = await _paymentRepository.Where(x =>
                    x.TenantId == model.TenantId &&
                    x.Status == SubscriptionStatus.Active &&
                    !x.IsDeleted
                ).AnyAsync();

            if (hasActiveSub)
            {
                return ServiceResult<string>.Fail("Firmanın zaten AKTİF bir aboneliği var. Yeni ekleme yapamazsınız. Mevcut aboneliğe gün ekleme/çıkarma işlemi yapabilirsiniz.", 400);
            }

            // --- TARİH HESAPLAMA (Sadece Monthly/Yearly) ---
            // Aktif abonelik olmadığı için (Strict Mode) başlangıç HER ZAMAN ŞİMDİDİR.
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate;

            switch (model.PlanType)
            {
                case SubscriptionPlanType.Monthly:
                    endDate = startDate.AddMonths(1);
                    break;
                case SubscriptionPlanType.Yearly:
                    endDate = startDate.AddYears(1);
                    break;
                default:
                    // Fallback olarak 1 ay ekle (veya hata fırlat)
                    endDate = startDate.AddMonths(1);
                    break;
            }

            var history = new TenantPaymentHistory
            {
                TenantId = tenant.Id,
                PlanType = model.PlanType, // Seçilen tip (Aylık/Yıllık)
                Status = SubscriptionStatus.Active, // Yeni başladığı için AKTİF
                Amount = model.Amount,
                IsPaid = model.IsPaid,
                StartDate = startDate,
                EndDate = endDate,
                Note = model.Note,
                CreatedByName = "Admin"
            };

            await _paymentRepository.AddAsync(history);

            // Tenant Ana Tabloyu Güncelle
            tenant.SubscriptionEndDate = endDate;
            if (!tenant.IsActive && model.IsPaid) tenant.IsActive = true;

            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();
            InvalidateBroadcastTenantList();

            return ServiceResult<string>.Success($"Paket tanımlandı. Bitiş: {endDate:dd.MM.yyyy}", 200);
        }

        public async Task<ServiceResult<string>> AdjustDaysAsync(AdjustSubscriptionDaysDto model)
        {
            // 1. Hedef Kaydı Bul
            var history = await _paymentRepository.GetByIdAsync(model.Id);
            if (history == null) return ServiceResult<string>.Fail("Abonelik kaydı bulunamadı.", 404);

            // 2. Firmayı Bul
            var tenant = await _tenantRepository.GetByIdAsync(history.TenantId);
            if (tenant == null) return ServiceResult<string>.Fail("Bağlı firma bulunamadı.", 404);

            // 3. Tarihi Güncelle (History Tablosu İçin)
            DateTime newDate = history.EndDate.AddDays(model.Days);
            history.EndDate = newDate;

            // 4. History Statüsünü Yönet (Burası kalabilir, History verisinin tutarlılığı için)
            if (newDate > DateTime.UtcNow)
                history.Status = SubscriptionStatus.Active;
            else
                history.Status = SubscriptionStatus.Expired;

            // Log Notu
            string islem = model.Days > 0 ? $"+{model.Days}" : $"{model.Days}";
            history.Note = (history.Note ?? "") + $" [Admin Düzenleme: {islem} gün]";

            _paymentRepository.Update(history);

            // -----------------------------------------------------------------------
            // 5. DÜZELTME: Tenant Tablosunu Senkronize Et (Sadece Tarihi)
            // -----------------------------------------------------------------------

            // A. Diğer kayıtların en büyük tarihini bul (Şu an düzenlediğimiz kayıt hariç)
            var maxDateFromOthers = await _paymentRepository
                .Where(x => x.TenantId == tenant.Id && x.Id != history.Id && !x.IsDeleted)
                .MaxAsync(x => (DateTime?)x.EndDate);

            // B. En ileri tarihi hesapla (Recalculate)
            DateTime finalMaxDate = newDate;

            if (maxDateFromOthers.HasValue && maxDateFromOthers.Value > newDate)
            {
                finalMaxDate = maxDateFromOthers.Value;
            }

            // C. Tenant'ı GÜNCELLE (Sadece Tarih)
            tenant.SubscriptionEndDate = finalMaxDate;

            // 🔥 DEĞİŞİKLİK BURADA:
            // Firma aktifliğini (ban durumunu) süreye bağlamıyoruz.
            // Eğer süresi bittiyse zaten Login olurken tarih kontrolüne takılacak.
            // tenant.IsActive = finalMaxDate > DateTime.UtcNow; // <--- BU SATIR SİLİNDİ

            // NOT: İstersen, "Süre uzatıldıysa ve firma şu an pasifse (banlı değilse) aktif et" gibi
            // özel bir mantık ekleyebilirsin ama en temiz hali hiç dokunmamaktır.

            _tenantRepository.Update(tenant);

            await _context.SaveChangesAsync();
            InvalidateBroadcastTenantList();

            return ServiceResult<string>.Success($"Kayıt güncellendi. Yeni Bitiş: {newDate:dd.MM.yyyy}", 200);
        }

        public async Task<ServiceResult<List<PaymentHistoryDto>>> GetHistoryByTenantIdAsync(string tenantId)
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId);
            if (tenant == null)
                return ServiceResult<List<PaymentHistoryDto>>.Fail("Firma bulunamadı.", 404);

            var historyList = await _paymentRepository
                .Where(x => x.TenantId == tenantId && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var dtos = historyList.Select(h => new PaymentHistoryDto
            {
                Id = h.Id,
                Status = h.Status, // 🔥 EKLENDİ: Status bilgisi dönülüyor
                PlanName = h.PlanType == SubscriptionPlanType.Monthly ? "Aylık" : "Yıllık",
                Amount = h.Amount,
                IsPaid = h.IsPaid,
                StartDate = h.StartDate,
                EndDate = h.EndDate,
                CreatedDate = h.CreatedAt,
                Note = h.Note
            }).ToList();

            return ServiceResult<List<PaymentHistoryDto>>.Success(dtos);
        }

        // ... Mevcut usingler ve class yapısı ...

        public async Task<ServiceResult<string>> DeleteSubscriptionAsync(string historyId)
        {
            // 1. Silinecek Kaydı Bul
            var historyToDelete = await _paymentRepository.GetByIdAsync(historyId);
            if (historyToDelete == null) return ServiceResult<string>.Fail("Kayıt bulunamadı.", 404);

            // 2. Firmayı Bul
            var tenant = await _tenantRepository.GetByIdAsync(historyToDelete.TenantId);
            if (tenant == null) return ServiceResult<string>.Fail("Firma bulunamadı.", 404);

            // 3. Önce Kaydı SİL (Veritabanından fiziksel olarak veya soft delete ile kaldır)
            // Silme işlemini önce yapıyoruz ki, birazdan "En büyük tarih kimde?" diye sorduğumuzda
            // bu kayıt artık cevapların içinde olmasın.
            _paymentRepository.Remove(historyToDelete);

            // Değişikliği veritabanına yansıt ki aşağıdaki sorguda bu kayıt gelmesin.
            // Transaction kullandığımız için eğer aşağıda hata olursa bu da geri alınır.
            await _context.SaveChangesAsync();
            InvalidateBroadcastTenantList();

            // -----------------------------------------------------------------------
            // 4. SENKRONİZASYON: Geriye kalan paketlerin en ileri tarihini bul
            // -----------------------------------------------------------------------

            var maxEndDate = await _paymentRepository
                .Where(x => x.TenantId == tenant.Id && !x.IsDeleted) // Silinen zaten gitti, kalanlara bakıyoruz
                .MaxAsync(x => (DateTime?)x.EndDate);

            // 5. Tenant Tablosunu Güncelle
            if (maxEndDate.HasValue)
            {
                // Başka paketler varsa, en ileri tarih hangisiyse firmanın tarihi o olur.
                tenant.SubscriptionEndDate = maxEndDate.Value;
            }
            else
            {
                // Hiç paket kalmadıysa (maxEndDate null döner)
                tenant.SubscriptionEndDate = null;
            }

            _tenantRepository.Update(tenant);
            await _context.SaveChangesAsync();
            InvalidateBroadcastTenantList();

            return ServiceResult<string>.Success("Abonelik silindi ve firma süresi yeniden hesaplandı.", 200);
        }
        // 2. GÜNCELLEME METODU
        public async Task<ServiceResult<string>> UpdateSubscriptionAsync(UpdateSubscriptionDto model)
        {
            var history = await _paymentRepository.GetByIdAsync(model.Id);
            if (history == null) return ServiceResult<string>.Fail("Kayıt bulunamadı.", 404);

            var tenant = await _tenantRepository.GetByIdAsync(history.TenantId);
            if (tenant == null) return ServiceResult<string>.Fail("Firma bulunamadı.", 404);

            // --- PLAN TİPİ DEĞİŞTİ Mİ? (Tarih Hesaplama) ---
            if (history.PlanType != model.PlanType)
            {
                TimeSpan oldDuration = history.EndDate - history.StartDate;

                DateTime newLocalEndDate = model.PlanType == SubscriptionPlanType.Monthly
                    ? history.StartDate.AddMonths(1)
                    : history.StartDate.AddYears(1);

                TimeSpan newDuration = newLocalEndDate - history.StartDate;
                TimeSpan difference = newDuration - oldDuration;

                if (tenant.SubscriptionEndDate.HasValue)
                {
                    tenant.SubscriptionEndDate = tenant.SubscriptionEndDate.Value.Add(difference);
                }

                history.EndDate = newLocalEndDate;
            }

            // --- GÜNCELLEMELER ---

            // 🔥 EKLENDİ: Status güncellemesi
            // Kullanıcı manuel olarak 'Expired' veya 'Active' yapabilir.
            history.Status = model.Status;

            history.PlanType = model.PlanType;
            history.Amount = model.Amount;
            history.IsPaid = model.IsPaid;
            history.Note = model.Note;

            _paymentRepository.Update(history);
            _tenantRepository.Update(tenant);

            await _context.SaveChangesAsync();
            InvalidateBroadcastTenantList();

            return ServiceResult<string>.Success("Abonelik güncellendi.", 200);
        }

        // 3. TEKİL GETİRME (Edit Sayfası İçin)


        public async Task<ServiceResult<PaymentHistoryDto>> GetSubscriptionByIdAsync(string id)
        {
            var h = await _paymentRepository.GetByIdAsync(id);
            if (h == null) return ServiceResult<PaymentHistoryDto>.Fail("Kayıt bulunamadı", 404);

            var dto = new PaymentHistoryDto
            {
                Id = h.Id,
                Status = h.Status, // 🔥 EKLENDİ
                PlanName = h.PlanType == SubscriptionPlanType.Monthly ? "Aylık" : "Yıllık",
                Amount = h.Amount,
                IsPaid = h.IsPaid,
                StartDate = h.StartDate,
                EndDate = h.EndDate,
                CreatedDate = h.CreatedAt,
                Note = h.Note
            };

            return ServiceResult<PaymentHistoryDto>.Success(dto);
        }
    }
}