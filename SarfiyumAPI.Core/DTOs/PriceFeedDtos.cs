using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SarfiyumAPI.Core.DTOs
{
    /// <summary>
    /// İstemciye giden fiyat satırı.
    ///
    /// ⚠️ Bu, Datshop'tan GELEN <see cref="PriceData"/> ile karıştırılmamalıdır.
    /// İkisi ayrı tutuluyor çünkü biri dış sağlayıcının formatı (değiştiremeyiz),
    /// diğeri bizim istemcilerimizle olan sözleşmemiz (istediğimiz gibi şekillendiririz).
    /// Eskiden tek sınıf ikisini birden temsil ediyordu ve bu yüzden istemciye
    /// Datshop'a ait alanlar (`source`) ve ürün adının ikinci bir kopyası
    /// (`description`) gidiyordu.
    /// </summary>
    public class PriceItemDto
    {
        /// <summary>
        /// Ürünün kalıcı kimliği (<c>TenantProduct.Id</c>).
        ///
        /// Bu alan olmadığı için istemciler ürünleri "KATEGORİADI_ÜRÜNADI" gibi
        /// string anahtarlarla eşleştiriyordu; ürün adı değişince görünürlük/ayar
        /// eşleşmesi sessizce bozuluyordu ve aynı isimli iki ürün çakışıyordu
        /// (web W13, mobil M7). Delta yayınında da satırların kimliği budur.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        /// <summary>Kuyumcunun verdiği ürün adı (ekranda görünen).</summary>
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = default!;

        /// <summary>
        /// Ürünün beslendiği Datshop sembolü (<c>USDTRY</c>, <c>ALTIN</c> ...).
        ///
        /// Mobil, DOLAR/EURO özet başlığını bulmak için ürün ADINDA metin araması
        /// yapıyordu; kuyumcu ürüne "DOLAR BOZDURMA" deyince yanlış ürün başlığa
        /// çıkıyordu (mobil M2). Artık kaynak sembol üzerinden eşleşilebilir.
        /// </summary>
        [JsonPropertyName("sourceKey")]
        public string SourceKey { get; set; } = default!;

        [JsonPropertyName("categoryIndex")]
        public int CategoryIndex { get; set; }

        /// <summary>Kategori adı. Yalnızca snapshot'ta gönderilir, delta'da gönderilmez.</summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("bid")]
        public decimal? Bid { get; set; }

        [JsonPropertyName("ask")]
        public decimal? Ask { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }
    }

    /// <summary>
    /// Yalnızca fiyatı değişen satırlar için gönderilen hafif kayıt.
    /// Kimlik ve statik alanlar (ad, kategori, sıra) burada YOKTUR — onlar snapshot'ta gelir
    /// ve yalnızca kuyumcu ürün ayarlarını değiştirdiğinde değişir; o durumda zaten
    /// delta yerine yeni bir snapshot gönderilir.
    /// </summary>
    public class PriceChangeDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = default!;

        [JsonPropertyName("bid")]
        public decimal? Bid { get; set; }

        [JsonPropertyName("ask")]
        public decimal? Ask { get; set; }
    }

    /// <summary>
    /// Tam liste mesajı (<c>ReceivePriceSnapshot</c>).
    /// Bağlantı anında, ürün yapılandırması değiştiğinde ve periyodik olarak gönderilir.
    /// </summary>
    public class PriceSnapshotDto
    {
        /// <summary>
        /// Bu mesajın ait olduğu yayın akışı (grup adı).
        ///
        /// Neden gerekli: <c>seq</c> numaraları GRUP BAŞINA ilerliyor. Bir viewer'a özel fiyat
        /// tanımlandığında istemci standart gruptan özel gruba taşınıyor ve iki grubun sıra
        /// numaraları birbirinden tamamen bağımsız. İstemci tek bir <c>lastSeq</c> tuttuğu için,
        /// taşınma anında yolda olan eski gruba ait bir delta (yüksek seq) yeni grubun
        /// snapshot'ından (düşük seq) sonra ulaşırsa hem yanlış fiyat uygulanır hem de
        /// sonraki tüm doğru delta'lar "eski" sanılıp yok sayılır — ekran periyodik tam
        /// senkrona kadar donardı.
        ///
        /// İstemci kuralı: <c>feed</c> değişince sıra takibi sıfırlanır; farklı bir feed'e ait
        /// delta hiç uygulanmaz.
        /// </summary>
        [JsonPropertyName("feed")]
        public string Feed { get; set; } = default!;

        /// <summary>Mesaj başına TEK zaman damgası (eskiden her satırda tekrar ediyordu).</summary>
        [JsonPropertyName("ts")]
        public string Timestamp { get; set; } = default!;

        /// <summary>
        /// Grup bazlı artan sıra numarası. İstemci bir delta'da sıra atlaması görürse
        /// elindeki listenin sapmış olabileceğini anlar ve yeniden bağlanabilir.
        /// </summary>
        [JsonPropertyName("seq")]
        public long Sequence { get; set; }

        [JsonPropertyName("items")]
        public List<PriceItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Değişiklik mesajı (<c>ReceivePriceDelta</c>). Yalnızca fiyatı oynayan satırları taşır.
    /// </summary>
    public class PriceDeltaDto
    {
        /// <inheritdoc cref="PriceSnapshotDto.Feed"/>
        [JsonPropertyName("feed")]
        public string Feed { get; set; } = default!;

        [JsonPropertyName("ts")]
        public string Timestamp { get; set; } = default!;

        [JsonPropertyName("seq")]
        public long Sequence { get; set; }

        [JsonPropertyName("changed")]
        public List<PriceChangeDto> Changed { get; set; } = new();
    }
}
