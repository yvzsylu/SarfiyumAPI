using SarfiyumAPI.Core.ValueObjects;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public class Tenant : EntityBase
    {
        public string Name { get; set; } = default!;
        public string TaxNumber { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true; // Varsayılan Aktif

        // BEST PRACTICE: Adres Bilgisi
        // Veritabanında ayrı bir tablo olmaz, Tenants tablosunda 
        // Address_City, Address_Street gibi kolonlar oluşur.
        public Address Address { get; set; } = new Address();

        // İSİM DEĞİŞİKLİĞİ: Employees -> Users
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        // 1. Kritik Alan: Abonelik Bitiş Tarihi
        // Null ise: Hiç abonelik başlatmamış demektir.
        public DateTime? SubscriptionEndDate { get; set; }

        // 2. Navigation Property: Geçmiş ödemelere erişmek için
        // İlişkiyi kuruyoruz (One-to-Many)
        public ICollection<TenantPaymentHistory> PaymentHistories { get; set; } = new List<TenantPaymentHistory>();

        // --- PUBLIC (GİRİŞSİZ) FİYAT EKRANI ---

        /// <summary>
        /// Bu tenant'ın fiyatları girişsiz olarak yayınlanır mı?
        ///
        /// Varsayılan <c>false</c>: özellik açıkça açılmadıkça hiçbir tenant'ın fiyatları
        /// anonim erişime açılmaz. Kapalıyken public yayın listesi hiç hesaplanmaz, yani
        /// kapalı tenant'lar için sıfır ek maliyet.
        /// </summary>
        public bool IsPublicFeedEnabled { get; set; } = false;

        /// <summary>
        /// Public ekranın tenant'ı tanımasını sağlayan kısa ve tahmin edilemez anahtar
        /// (örn. <c>altinkose</c>). Mobil uygulama build zamanında bunu taşır ve
        /// <c>GET /api/public/tenant/{slug}</c> ile tenant'ı çözer.
        ///
        /// Neden tenant id'si değil: id doğrudan gömülseydi APK içinden okunabilir hale
        /// gelirdi. Slug ise yalnızca public yayına erişim verir, başka hiçbir şeye yaramaz
        /// ve değiştirilmesi kolaydır.
        /// </summary>
        [StringLength(64)]
        public string? PublicSlug { get; set; }


        // --- OPSİYONEL HELPER (Veritabanına kaydedilmez) ---
        // Kod içinde hızlıca kontrol yapmak için bu property'i kullanabilirsin.
        // Login olurken: if(!tenant.IsSubscriptionActive) throw ... diyebilmek için.
        public bool IsSubscriptionActive
        {
            get
            {
                // Tarih varsa VE şu anki zamandan büyükse (gelecekteyse) aktiftir.
                return SubscriptionEndDate.HasValue && SubscriptionEndDate.Value > DateTime.UtcNow;
            }
        }
    }
}
