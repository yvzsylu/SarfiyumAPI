using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SarfiyumAPI.Core.DTOs
{
    /// <summary>
    /// Girişsiz (public) fiyat ekranının açılışta ihtiyaç duyduğu tenant bilgisi.
    ///
    /// ⚠️ Bu uç nokta ANONİM erişime açıktır. Buraya yalnızca zaten mağaza vitrininde
    /// görünen türden bilgiler konur: firma adı, logo ve kategori başlıkları.
    /// Tenant kimliği (<c>Id</c>), abonelik durumu, iletişim/vergi bilgisi gibi alanlar
    /// **kasıtlı olarak dışarıda bırakılmıştır.**
    /// </summary>
    public class PublicTenantDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("categories")]
        public List<PublicCategoryDto> Categories { get; set; } = new();
    }

    public class PublicCategoryDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }
    }
}
