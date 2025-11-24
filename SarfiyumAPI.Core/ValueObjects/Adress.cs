using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.ValueObjects
{
    // [Owned] attribute'ü EF Core'a "Bu sınıfın kendi tablosu olmasın, 
    // kullanıldığı tablonun kolonları olarak gömülsün" der.
    [Owned]
    public class Address
    {
        public string Street { get; set; } = default!;   // Sokak/Cadde/Mahalle
        public string City { get; set; } = default!;     // İl
        public string District { get; set; } = default!; // İlçe
        public string Country { get; set; } = "Türkiye"; // Ülke
        public string? ZipCode { get; set; }             // Posta Kodu
        public string? FullAddress { get; set; }         // Açık Adres (Textarea)

        // Yardımcı bir metot: Adresi tek satırda yazdırmak için
        public override string ToString()
        {
            return $"{FullAddress} {District}/{City} - {Country}";
        }
    }
}
