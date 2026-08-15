# AGENTS.md — SarfiyumAPI (.NET 8 Backend)

> Bu dosya, Sarfiyum ekosisteminde çalışan **tüm AI ajanları** (Claude Code, Gemini/Antigravity, Codex vb.) ve insan geliştiriciler için tek kaynak dokümandır.
> `CLAUDE.md` bu dosyaya işaret eder; içerik **sadece burada** tutulur.

---

## 0. DEĞİŞMEZ KURALLAR (Her ajan bunlara uymak zorunda)

1. **Tek doküman kaynağı `AGENTS.md`'dir.** `CLAUDE.md` sadece bu dosyaya yönlendiren bir pointer'dır, içerik barındırmaz. Aynı kural üç repoda da geçerlidir.
2. **Her tamamlanan işten sonra ilgili repo(lar)ın `AGENTS.md` dosyası güncellenir.** Yeni özellik, kaldırılan özellik, değişen mimari, çözülen/eklenen bulgu → dokümana yansır. Bu opsiyonel değildir, işin bir parçasıdır.
3. **Üç repo tek üründür.** Bir tarafta yapılan değişikliğin diğer iki tarafa etkisi *her zaman* değerlendirilir ve cevapta açıkça belirtilir:
   - `SarfiyumAPI` → `c:\Users\yvzsy\source\repos\SarfiyumAPI` (.NET 8)
   - `SarfiyumUI` → `c:\Users\yvzsy\OneDrive\Desktop\SarfiyumUI` (Angular 19)
   - `sarfiyum_mobile` → `c:\Users\yvzsy\OneDrive\Desktop\sarfiyum_mobile` (Flutter)
4. **`master`/`main` push = canlıya deploy.** GitHub Actions otomatik build alıp Ubuntu sunucuya (4 çekirdek / 6 GB RAM) deploy eder. Doğrudan ana branch'e push edilmez; işler feature branch üzerinden yapılır ve kullanıcı onayı olmadan push/deploy **yapılmaz**.
5. **Kullanıcı açıkça istemeden kod değiştirilmez.** İnceleme/analiz istendiğinde sadece analiz yapılır, dosya düzenlenmez.
6. **Migration üretmek, veritabanı şeması değiştirmek, seed data eklemek → önce kullanıcıya sorulur.** Canlı DB tektir ve paylaşımlıdır.
7. **Sır (secret) asla koda gömülmez.** Yeni bir anahtar/parola gerektiğinde environment variable veya `docker-compose` üzerinden verilir. (Mevcut durum §7'de, kritik borç olarak kayıtlıdır.)
8. **Fiyat yayını (hub/broadcast) hattına dokunulan her değişiklikte performans etkisi ölçülür ve yazılır.** Bu hat ürünün kalbidir ve sunucu kaynağı sınırlıdır.
9. Kod içi yorumlar ve kullanıcıya dönülen mesajlar **Türkçe**; sınıf/metot/değişken isimleri **İngilizce** (mevcut kod tabanının stili budur).
10. **Tenant izolasyonu kutsaldır.** Her sorgu `TenantId` ile filtrelenir; `TenantId` her zaman JWT claim'inden alınır, istemciden gelen gövdeye asla güvenilmez.

---

## 1. ÜRÜN NEDİR?

Sarfiyum, **kuyumcular için çok kiracılı (multi-tenant) canlı fiyat yönetim platformudur.**

- Piyasa verisi dış sağlayıcı **Datshop**'tan (`https://sse.datshop.com.tr`) Socket.IO ile anlık çekilir.
- Backend bu ham veriyi **tenant'ın kendi çarpanlarıyla (milyem)** ve **makasıyla (addon)** işleyip SignalR üzerinden yayınlar.
- Kuyumcu (tenant) mağaza içi TV'de **Dashboard**'ı gösterir; müşterileri **mobil uygulamadan** kendi hesaplarıyla fiyatları izler.
- Kuyumcu, tek tek müşterileri (**Viewer**) için ürün bazında **özel çarpan/makas** tanımlayabilir.
- **SuperAdmin** (Admin rolü) firmaları açar, abonelikleri yönetir, genel özeti görür.
- **Aynı anda tek cihaz kuralı:** Bir kullanıcı Web'de bir, Mobil'de bir oturum açabilir. Yeni giriş eskisini anında düşürür.

### Roller

| Rol | Kim | Neler yapar |
|---|---|---|
| `Admin` | SuperAdmin (sen) | Firma açma, abonelik, kullanıcı/rol yönetimi, sistem ayarları, admin dashboard |
| `User` | Kuyumcu / firma sahibi | Ürün-çarpan-makas yönetimi, kategori adları, tema, viewer açma, viewer'a özel fiyat, logo |
| `Viewer` | Kuyumcunun müşterisi | Sadece fiyat ekranını görür. Token ömrü 5 dk, arka plana atınca çıkış yapar |

---

## 2. TEKNİK MİMARİ

### Katmanlar (solution: `SarfiyumAPI.sln`)

```
SarfiyumAPI          → Web API. Controllers, Hubs, Middleware, HostedService, Program.cs
SarfiyumAPI.Service  → İş mantığı (Services/*, Jobs/ExpirationJob)
SarfiyumAPI.Data     → AppDbContext, Migrations, Repository<T>
SarfiyumAPI.Core     → Entities, DTOs, Enums, Interfaces, ServiceResult<T>
SarfiyumAPI.Entity   → ⚠️ BOŞ / ÖLÜ PROJE (sadece Class1.cs)
```

### Ana teknolojiler
.NET 8 · EF Core 8 (SQL Server) · ASP.NET Identity · JWT Bearer · SignalR · Coravel (scheduler) · SocketIOClient 3.1.2 · MailKit · IMemoryCache · Swashbuckle

### İstek akışı
```
Client → CORS → HttpsRedirection → StaticFiles → Authentication
      → SessionCheckMiddleware (tek cihaz kontrolü, HER istekte DB sorgusu)
      → Authorization → Controller → Service → Repository<T> → AppDbContext
```

### Fiyat yayın akışı (ürünün kalbi)

**Yayın, gelen paket frekansından bağımsızdır (tick tabanlı).** Paketler yalnızca önbelleği
günceller; sabit aralıklı bir döngü son durumu yayınlar. Yeni veri yoksa tick boşa döner.

```
Datshop Socket.IO ── "price_changed" ──► _priceCache güncelle + "kirli" bayrağı
                                          (ConcurrentDictionary<Symbol, PriceData>)

RunBroadcastLoopAsync  ── her 1000 ms (SocketServer:BroadcastIntervalMs) ──►
                          kirli değilse atla, kirliyse:
                                          └─ BroadcastToTenants()
                                               ├─ TenantService.GetActiveTenantIdsAsync()      [cache 10 dk]
                                               └─ her tenant için:
                                                    ├─ MultiplierSettingsService.GetProductsAsync()  [cache 1 sa]
                                                    ├─ CalculatePriceList(standart)  → Group "{tenantId}_Standard"
                                                    ├─ VisitorSettingService.GetVisitorsWithSpecialSettingsAsync()  [cache 1 sa]
                                                    └─ her özel viewer için:
                                                         ├─ BAĞLI DEĞİLSE ATLA (IHubConnectionRegistry)
                                                         └─ CalculatePriceList(özel)  → Group "{visitorId}"
```

Ayrıca bir **watchdog döngüsü** (5 sn'de bir kontrol) soketin sessizce kopmasını yakalar;
ardışık sıfırlamalarda üstel geri çekilme uygular (piyasa kapalıyken reconnect fırtınası olmasın).

**Fiyat formülü:**
```
finalBid = kaynakBid × BuyMultiplier  − AddonAmount     (alış)
finalAsk = kaynakAsk × SellMultiplier + AddonAmount     (satış)
```
Viewer'a özel ayar varsa `SpecialBuyMultiplier` / `SpecialSellMultiplier` / `SpecialAddonAmount` bu değerleri **ezer** (null olanlar tenant değerinde kalır).

### İstemciye giden yayın sözleşmesi

| Event | Ne zaman | İçerik |
|---|---|---|
| `ReceivePriceSnapshot` | Bağlantı anında, grup değişince, ürün yapılandırması değişince, her 60 sn'de bir | `{ feed, ts, seq, items[] }` — tam liste |
| `ReceivePriceDelta` | Aradaki tick'lerde, yalnızca fiyat oynadıysa | `{ feed, ts, seq, changed[] }` — sadece `{id, bid, ask}` |
| `ForceCheckSession` | Yeni giriş / çıkış | `(sessionId, clientSource)` |

`PriceItemDto`: `id` (= `TenantProduct.Id`), `symbol`, `sourceKey`, `categoryIndex`,
`category`, `bid`, `ask`, `orderIndex`. Fiyatlar **4 haneye yuvarlanır**.
Her iki mesaj da `feed` (yayın grubu adı), `ts` ve `seq` taşır.

> ⚠️ **İstemcilerin uymak zorunda olduğu üç kural:**
> 1. **Snapshot gelmeden önce ulaşan delta'lar yok sayılır.**
> 2. **`feed` mevcut akıştan farklıysa delta yok sayılır.**
> 3. **`seq` snapshot'takinden küçük veya eşitse delta yok sayılır.**
>
> 1 ve 3'ün sebebi: bağlantı anında hub'ın doğrudan gönderdiği snapshot ile gruba giden
> yayının sırası garanti değildir.
>
> **2'nin sebebi daha ince ve kritik:** `seq` numaraları GRUP BAŞINA ilerler. Bir viewer'a
> özel fiyat tanımlandığında istemci standart gruptan özel gruba taşınır ve iki grubun sıra
> numaraları birbirinden bağımsızdır (standart grup sunucu açılışından beri sayıyor, yeni
> özel grup 1'den başlıyor). Taşınma anında yolda olan eski gruba ait bir delta, yeni grubun
> snapshot'ından sonra ulaşırsa hem **yanlış fiyat** uygulanır hem de `lastSeq` çok yükseğe
> çıktığı için **sonraki tüm doğru delta'lar yok sayılır** — ekran periyodik tam senkrona
> kadar (60 sn'ye kadar) donardı. `feed` alanı bu yarışı kapatır.
>
> Ayrıca istemci **yeniden bağlanırken** yerel durumunu sıfırlamalıdır.

> ⚠️ Dinleyiciler `start()` çağrılmadan **önce** bağlanmalıdır; sunucu ilk tam listeyi
> `OnConnectedAsync` içinde gönderdiği için sonradan kaydedilen dinleyici onu kaçırır.

### SignalR grup şeması

Gruplar **platform bazlıdır**, çünkü görünürlük filtresi (`ShowOnWeb`/`ShowOnMobile`)
sunucuda uygulanıyor ve web ile mobil farklı ürün kümesi görebiliyor. Platform token'daki
`ClientSource` claim'inden çözülür.

| Grup adı | Kim girer | Ne alır |
|---|---|---|
| `{tenantId}_Std_Web` / `{tenantId}_Std_Mobile` | Özel fiyat ayarı **olmayan** kullanıcılar | Tenant'ın standart fiyat listesi |
| `{visitorId}_Web` / `{visitorId}_Mobile` | Özel fiyat ayarı **olan** viewer'lar | Kişiye özel hesaplanmış liste |
| `{userId}` | **Her** bağlanan kullanıcı | Yalnızca **sinyal** grubu (`ForceCheckSession`) — fiyat gitmez |

Aynı viewer web ve mobilde **aynı anda** bağlı olabilir (tek cihaz kuralı ikisi için ayrı
işler), bu yüzden `IHubConnectionRegistry` bağlantı başına platformu da tutar ve yayıncı
yalnızca **gerçekten bağlanılmış** platformlar için liste hesaplar.

**Grup üyeliği canlı olarak senkron tutulur.** İlk karar `GoldHub.OnConnectedAsync`'te verilir; sonrasında kuyumcu özel fiyat ekler/silerse üyelik anında güncellenir:

```
IHubConnectionRegistry   (singleton)   UserId -> ConnectionId kümesi
   ▲ Add                                   ▲ Remove
   └─ GoldHub.OnConnectedAsync             └─ GoldHub.OnDisconnectedAsync

VisitorSettingService.UpsertSettingAsync ─► IViewerPricingNotifier.MoveToSpecialAsync
                                              └─► RemoveFromGroup(conn, "{tenantId}_Standard")

VisitorSettingService.DeleteSettingAsync ─► (başka ayarı kalmadıysa)
                                            IViewerPricingNotifier.MoveToStandardAsync
                                              └─► AddToGroup(conn, "{tenantId}_Standard")
```

Servis katmanı SignalR'ı tanımadığı için `ISessionNotificationService` ile aynı desen kullanılır: arayüz `Core`'da, implementasyon (`ViewerPricingNotifier`) API katmanındadır. Viewer o an bağlı değilse işlem no-op'tur; bir sonraki bağlantıda `OnConnectedAsync` zaten doğru grubu seçer.

### Ortamlar
- **Development** → `MockGoldPriceHostedService` (sahte fiyat, 2 sn'de bir), `DevCors` (her origin), Swagger açık.
- **Production** → `GoldPriceHostedService` (gerçek Datshop), `ProdCors` (whitelist: `sarfiyum.com`, `www.sarfiyum.com`, `portal.cilcilaltin.com`, `portal.altinkosenadirkuyumculuk.com`).

---

## 3. VERİ MODELİ

`EntityBase` (tüm entity'lerin tabanı): `Id` (Guid string, 36), `CreatedByName/Id`, `ModifiedByName/Id`, `RemovedByName/Id`, `CreatedAt`, `ModifiedAt`, `RemovedAt`, `IsDeleted`.

| Entity | Açıklama | Önemli alanlar |
|---|---|---|
| `AppUser : IdentityUser` | Kullanıcı | `FullName`, `IsActive`, `TenantId?` |
| `Tenant` | Kuyumcu firması | `Name`, `TaxNumber`, `PhoneNumber`, `Website`, `LogoUrl`, `IsActive`, `Address` (owned), `SubscriptionEndDate?` |
| `TenantProductCategory` | Tenant'a özel kategori | `Name`, `OrderIndex`, `IsActive` |
| `TenantProduct` | Tenant'ın ürünü | `SourceKey` (Datshop sembolü), `Name`, `BuyMultiplier`, `SellMultiplier`, `AddonAmount` (hepsi `decimal(18,10)`), `OrderIndex`, `IsActive`, `ShowOnWeb`, `ShowOnMobile` |
| `VisitorProductSetting` | Viewer'a özel fiyat | `VisitorId`, `TenantProductId`, `SpecialBuy/Sell Multiplier`, `SpecialAddonAmount`, `IsActive` |
| `TenantSetting` | Tenant ayarları (TenantId unique) | `WhatsAppNumber`, `DashboardTheme`, `UiSettingsJson`, `CustomColorsJson` |
| `TenantPaymentHistory` | Abonelik/ödeme | `PlanType` (Monthly/Yearly), `Status` (Active/Expired/Cancelled), `Amount decimal(18,2)`, `IsPaid`, `StartDate`, `EndDate`, `Note` |
| `UserActiveSession` | Tek cihaz kontrolü | `UserId`, `TenantId`, `ClientSource` (Web=1/Mobile=2), `SessionId`, `LastLoginDate` |
| `UserLoginHistory` | Giriş logu | `IpAddress` (X-Forwarded-For), `UserAgent`, `LoginDate`, `IsSuccess` |
| `SystemCatalog` | Datshop sembol kataloğu (66 seed kayıt) | `SourceKey`, `Name` |
| `AppSetting` | Global SMTP ayarları | `SmtpHost/Port/Email/Password/EnableSsl/DisplayName` |

**Soft delete query filter:** sadece `Tenant`, `TenantProductCategory`, `TenantProduct`, `TenantPaymentHistory` üzerinde tanımlı.

**Yeni tenant açılışında otomatik seed:** 5 kategori (Maden, Döviz, Bilezik, Ata & Reşat, Kredi Kartı) + ~27 varsayılan ürün (`TenantService.CreateAsync`).

---

## 4. MEVCUT ÖZELLİKLER (Backend — eksiksiz liste)

### 4.1 Kimlik & Oturum (`AuthController`, `AuthService`)
- `POST /api/auth/login` — kullanıcı adı + şifre + `clientSource` (Web=1 / Mobile=2). JWT üretir.
  - Kullanıcı pasifse 403, firma pasifse 403, abonelik yoksa/dolmuşsa 403.
  - **Token ömrü:** `Viewer` → 5 dakika, diğerleri → 6 ay.
  - **Claim'ler:** `sub`, `jti`, `userId`, `email`, `fullName`, `SessionId`, `ClientSource`, `tenantId`, `tenantName`, `tenantLogoUrl`, `subscriptionEndDate`, `roles`.
  - **Tek cihaz:** `UserActiveSession` (UserId + ClientSource) upsert edilir, yeni `SessionId` üretilir.
  - **Anlık kick:** `SignalRSessionService.SendForceCheckSession(userId, newSessionId, clientSource)` → eski cihaz kendini atar.
  - Her başarılı girişte `UserLoginHistory` kaydı.
- `POST /api/auth/reset-password` — e-postaya rastgele 8 karakterli yeni şifre gönderir. **Viewer rolü şifre sıfırlayamaz.**
- `POST /api/auth/logout` — `[Authorize]`, gövde almaz. Token'daki `userId` / `SessionId` / `ClientSource` / `TenantId` claim'lerinden çalışır.
  - `UserActiveSession` satırını siler (yalnızca token'daki `SessionId` DB'dekiyle **eşleşiyorsa** — eski bir token yeni bir oturumu kapatamaz).
  - `IActiveSessionCache` kaydını düşürür.
  - `ForceCheckSession(userId, "", clientSource)` yollar → istemci kendini atar. Diğer istemci tipi (örn. web'den çıkarken mobil) etkilenmez, çünkü istemciler `clientSource`'u karşılaştırır.
  - Kullanıcının açık SignalR bağlantılarını `{userId}` ve `{tenantId}_Standard` gruplarından çıkarır → istemci sinyali yok saysa bile fiyat akışı **sunucuda** kesilir.
  - İdempotenttir: zaten kapalı oturumda da başarı döner.
- `SessionCheckMiddleware` — her authenticated istekte token'daki `SessionId`'yi geçerli oturumla karşılaştırır, uyuşmazsa 401. Karşılaştırma **`IActiveSessionCache` üzerinden** yapılır (5 dk önbellek, login/logout'ta noktasal güncellenir) — her istekte DB'ye gitmez.
- `GoldHub.OnConnectedAsync` — aynı kontrolü SignalR bağlantısında yapar (aynı önbellek), uyuşmazsa `Context.Abort()`. Viewer için token bitiş süresinde otomatik disconnect zamanlar.

### 4.2 SuperAdmin (`/api/admin/*`, `[Authorize(Roles="Admin")]`)
- **Tenant** (`TenantController` / `TenantService`): listele, ID ile getir, oluştur, güncelle, soft-delete, aktif/pasif; `create-with-user` (firma + ilk `User` hesabını tek seferde açar, hata olursa rollback); logo yükleme (`wwwroot/uploads/logos`).
- **Abonelik** (`SubscriptionController` / `SubscriptionService`): abonelik ekle (aylık/yıllık; aktif abonelik varken yeni eklenemez), geçmiş listele, tek kayıt getir, güncelle, sil, `adjust-days` (gün ekle/çıkar + Tenant tarihini yeniden hesapla).
- **Kullanıcı** (`AccountController` / `AccountService`): kullanıcı oluştur, listele, ID ile getir, güncelle, rol ata / rolleri güncelle, aktif-pasif, sil.
- **Rol** (`RoleController` / `RoleService`): rol listele/oluştur/güncelle/sil.
- **Dashboard** (`DashboardController` / `DashboardService`): toplam firma / kullanıcı / aktif abonelik sayısı, toplam gelir, bekleyen tahsilat, aktif-expired dağılımı, aylık-yıllık plan dağılımı, son 6 ay gelir grafiği, ödemesi gecikenler (son 10), aboneliği 15 gün içinde bitecekler (ilk 5).
- **Ayarlar** (`SettingsController` / `SettingsService`): global SMTP ayarlarını oku/güncelle.
- **Zamanlanmış iş** (`ExpirationJob`, Coravel, her gün 00:00 yerel saat): `EndDate` geçmiş `Active` abonelikleri `Expired`'a çeker.

### 4.3 Kuyumcu / Tenant (`/api/customer/*`, `/api/user/*`)
- **Ürün** (`ProductController` / `MultiplierSettingsService`):
  - `GET product/list` — tüm ürünler (pasifler dahil). **Viewer da erişebilir.**
  - `GET product/categories` — kategori listesi (dropdown).
  - `GET product/catalog-list` — Datshop sembol kataloğu (`SystemCatalog`).
  - `POST product/create` — yeni ürün (kategori sahipliği doğrulanır, `OrderIndex` otomatik).
  - `PUT product/update-all` — toplu güncelleme (ad, çarpanlar, makas, kategori, aktiflik, web/mobil görünürlük).
  - `PATCH product/status/{id}` — aktif/pasif.
  - `PATCH product/toggle-web/{id}`, `PATCH product/toggle-mobile/{id}` — platform görünürlüğü.
  - `PATCH product/reorder` — sürükle-bırak sıralama.
  - `DELETE product/{id}` — sil.
  - Her yazma işleminden sonra `TenantProducts_{tenantId}` cache temizlenir.
- **Kategori** (`CategoryController` / `CategoryService`): listele (Viewer da görebilir), **sadece isim güncelleme** (ekleme/silme yok). İsim değişince ürün cache'i temizlenir.
- **Tenant ayarları** (`Customer/SettingsController` / `TenantSettingsService`):
  - `GET/PUT customer/settings` — WhatsApp numarası.
  - `GET/PUT customer/settings/theme` — `DashboardTheme`, `UiSettingsJson` (font boyutu/kalınlığı), `CustomColorsJson` (özel renk paleti). Okuma Viewer'a açık, yazma değil.
- **Viewer yönetimi** (`ViewerController` / `ViewerAccountService`): kendi tenant'ının viewer'larını listele, oluştur (rol zorla `Viewer`, TenantId token'dan), ID ile getir, güncelle (şifre dahil), aktif/pasif, sil. Tüm işlemlerde tenant sahipliği doğrulanır.
- **Viewer'a özel fiyat** (`VisitorSettingsController` / `VisitorSettingService`): `upsert` (ürün + viewer sahipliği doğrulanır), `delete/{visitorId}/{productId}`, `list/{visitorId}`.
- **Profil** (`ProfileController` / `ProfileService`): profil bilgisi, firma logosu yükleme.

### 4.4 Girişsiz (public) fiyat yayını

Tenant bazlı bir bayrakla açılır; **varsayılan kapalıdır** ve kapalı tenant'a sıfır ek maliyet getirir.

| Parça | Ne yapar |
|---|---|
| `Tenant.IsPublicFeedEnabled` | Yayının açık olup olmadığı. Varsayılan `false`. |
| `Tenant.PublicSlug` | Uygulamanın tenant'ı tanımasını sağlayan kısa anahtar (örn. `altinkose`). Tenant id'si APK'ya gömülmez. |
| `GET /api/public/tenant/{slug}` | `[AllowAnonymous]`. Firma adı, logo ve kategori başlıkları. Slug geçersiz / tenant pasif / abonelik bitmiş / bayrak kapalıysa **hepsi için aynı 404** döner (hangi slug'ın var olduğu sızmasın). ⚠️ Cevap **`ServiceResult` ile sarmalanır** — mobil `BaseApiService` gövdeyi `data` alanından okur; DTO'yu çıplak döndürmek istemcide sessizce boş veri olarak görünür. |
| `PublicPriceHub` (`/publichub`) | `[AllowAnonymous]`. Query'deki `slug`'ı çözer, `{tenantId}_Std_Public` grubuna abone eder, bağlantı anında tam listeyi gönderir. İstemciden çağrılabilen **hiçbir metodu yoktur**. |
| Yayıncı | Bayrağı açık tenant'lar için `PricePlatform.Public` listesi hesaplar ve `IHubContext<PublicPriceHub>` ile gönderir. |

**Fiyatlar tenant'ın STANDART çarpanlarıyla hesaplanır**; viewer'a özel ayar public'te asla uygulanmaz.
Ürün kapsamı şimdilik `ShowOnMobile` bayrağını izler (ayrı bir `ShowOnPublic` gerekirse §11 soru 1).

**Abonelik kontrolü burada tek fren noktasıdır** — public'te login yok. `GetPublicFeedTenantIdsAsync`
aynı `SubscriptionEndDate` filtresini uygular, yani aboneliği biten tenant'ın girişsiz yayını da durur.

> ⚠️ **Ayrı hub olmasının sebebi:** `GoldHub` sınıf seviyesinde `[Authorize]` ve içinde oturum
> doğrulama, tek cihaz kuralı, viewer gruplaması ve otomatik disconnect var. Orayı
> `[AllowAnonymous]` yapıp içeride ayrıştırmak ürünün en hassas güvenlik akışını riske atardı.

> ⚠️ **nginx'te rate limiting hâlâ yapılmadı (§5.2 D7).** Anonim uç nokta açılmadan önce
> `limit_req_zone` / `limit_conn_zone` eklenmelidir.

### 4.5 Canlı yayın
- `GoldPriceHostedService` — Datshop Socket.IO bağlantısı, `price_changed` dinleme (yalnızca önbelleği günceller), **tick tabanlı yayın döngüsü**, watchdog döngüsü (üstel backoff'lu), overlap koruması, broadcast süresi ölçümü.
- `MockGoldPriceHostedService` — development'ta 19 sembol için sahte fiyat üretir (2 sn periyot). Karşılığı olmayan ürünler için sahte değer üretir ama **hesabı ortak `PriceCalculator` yapar**, böylece davranış canlıdan sapmaz.
- `PriceCalculator` (singleton) — **fiyat hesabının tek kaynağı.** Çarpan/makas, viewer override'ı, platform görünürlük filtresi ve yuvarlama burada.
- `PriceFeedStateTracker` (singleton) — grup başına son gönderilen durum; snapshot mı delta mı kararını verir, bağlantı anında verilecek tam listeyi tutar.
- `GoldHub` — `[Authorize]`. Bağlantıda oturum doğrulama, **platform bazlı gruplama**, bağlantı anında tam liste gönderme, Viewer için otomatik disconnect zamanlama. `OnDisconnectedAsync` bağlantıyı kayıt defterinden düşürür.
- `HubConnectionRegistry` (singleton) — UserId → (ConnectionId, Platform) eşlemesi. Grup senkronizasyonunun ve "yalnızca gerçekten bağlı olana yayın" optimizasyonunun temeli.
- `ViewerPricingNotifier` — özel fiyat ayarı değiştiğinde viewer'ın grup üyeliğini canlı günceller **ve yeni grubun mevcut listesini anında gönderir** (aksi halde istemci bir sonraki snapshot'a kadar eski listeyle kalırdı).
- İstemciye gönderilen event'ler: `ReceivePriceSnapshot`, `ReceivePriceDelta`, `ForceCheckSession(sessionId, clientSource)`.

### 4.6 Altyapı
- `Repository<T>` generic repo (IQueryable sızdırır).
- `ServiceResult<T>` sarmalayıcı (`IsSuccess`, `Data`, `Errors`, `StatusCode` [JsonIgnore]) → `CustomBaseController.CreateActionResult`.
- `EmailService` (MailKit, DB'deki SMTP ayarlarını kullanır).
- `MapFallbackToFile("index.html")` — Angular SPA aynı container'dan servis edilir (`wwwroot`).

---

## 5. DEPLOY HATTI

```
SarfiyumUI push (master/main)
      └─► .github/workflows/trigger.yml  ── repository_dispatch ──►
SarfiyumAPI push (master/main)
      └─► .github/workflows/deploy.yml
             1. API repo checkout
             2. SarfiyumUI repo checkout (secrets.GH_PAT) → ./SarfiyumUI
             3. Docker build (multi-stage: node:20 Angular build → dotnet sdk:8.0 publish → aspnet:8.0 runtime)
             4. Docker Hub push  → {DOCKERHUB_USERNAME}/sarfiyum-app:latest
             5. SSH deploy → /opt/sarfiyum
                docker compose pull app && up -d --force-recreate app
                rm -rf ./wwwroot/* && docker cp sarfiyum_backend:/app/wwwroot/. ./wwwroot/
                docker restart sarfiyum_nginx && docker image prune -f
```

- Dockerfile, build sırasında `environment.prod.ts` → `environment.ts` kopyalar (prod'da `apiUrl = '/api'`).
- Sunucu: **Ubuntu, 4 çekirdek, 6 GB RAM.** nginx + app container + (harici) SQL Server `212.87.221.91:1433`.
- Container env: `ASPNETCORE_URLS=http://+:8080`, `ASPNETCORE_ENVIRONMENT=Production`. Sırlar `docker-compose.yml` üzerinden `ConnectionStrings__Default`, `Jwt__Key`, `SocketServer__Key` ile ezilebilir.

> ⚠️ Deploy tek adımlıdır; **rollback mekanizması, health check ve staging ortamı yoktur.** `latest` tag'i üzerine yazılır, bozuk build doğrudan canlıya çıkar.

### 5.1 Sunucu yapılandırması (`/opt/sarfiyum`)

Bu dosyalar **repoda değildir**, sunucuda elle yönetilir. İçerikleri 2026-08-15'te incelendi.

- `docker-compose.yml` — 3 servis: `app` (sarfiyum_backend), `portfolio` (yavuzsoylu_frontend), `nginx` (sarfiyum_nginx). Hepsi `sarfiyum-net` bridge ağında. Sırlar (`ConnectionStrings__Default`, `Jwt__Key`, `SocketServer__Key`) environment olarak burada; git'te değil.
- `nginx.conf` — tek dosyada 5 site: `sarfiyum.com`, `portal.cilcilaltin.com`, `portal.altinkosenadirkuyumculuk.com`, `yavuzsoylu.com`, `eventmoments.app`. İlk üçü **aynı** `wwwroot`'u (aynı Angular build'i) servis eder; ayrım backend'de JWT `tenantId` ile yapılır. `eventmoments_*` container'ları başka bir compose projesinden gelir.

### 5.2 BULGULAR — ALTYAPI (sunucu tarafı, kod değil)

| # | Bulgu | Detay | Şiddet |
|---|---|---|---|
| ~~**D1**~~ | ~~**Docker log rotation yok**~~ | → ✅ **2026-08-15'te sunucuda uygulandı.** `docker-compose.yml`'e `x-logging` anchor'ı (`max-size: 10m`, `max-file: 5`) eklendi ve `app`, `portfolio`, `nginx` servislerine bağlandı → container başına tavan 50 MB. En hızlı büyüyen nginx'ti: resmi imajda `access.log → /dev/stdout` olduğu için her HTTP isteği json-file'a düşüyordu. (Kod ayağı = P9, Adım 2. Eski adı **P9b**.) | ✅ |
| ~~**D2**~~ | ~~**`client_max_body_size` tanımlı değil → logo yükleme 1 MB'ta 413 veriyor**~~ | → ✅ **2026-08-15'te sunucuda uygulandı.** `http {}` bloğuna `client_max_body_size 10M;` eklendi; tüm site bloklarını kapsıyor. Öncesinde nginx varsayılanı 1 MB'tı ve 1 MB üstü firma logosu backend'e hiç ulaşmadan reddediliyordu. Mobil ayağı ayrıca `sarfiyum_mobile` **M15** ile çözüldü (görsel istemcide küçültülüyor). | ✅ |
| ~~**D3**~~ | ~~**`proxy_pass` DNS'i açılışta çözüyor → her deploy'da 502 penceresi**~~ | → ✅ **2026-08-15'te sunucuda uygulandı.** 3 sarfiyum server bloğundaki `/api/` ve `/goldhub` ile yavuzsoylu bloğundaki 3 location `set $degisken ...; proxy_pass http://$degisken;` desenine geçirildi; nginx artık `resolver 127.0.0.11 valid=10s` üzerinden **çalışma anında** çözüyor. **Takip işi:** `deploy.yml`'deki `docker restart sarfiyum_nginx` satırı artık gereksiz — `nginx -s reload`'a indirilebilir veya kaldırılabilir (bkz. §5.3). | ✅ |
| ~~**D4**~~ | ~~**`/api/` bloklarında koşulsuz `Connection 'upgrade'`**~~ | → ✅ **2026-08-15'te sunucuda uygulandı.** `http {}` içine `map $http_upgrade $connection_upgrade { default upgrade; '' close; }` eklendi; tüm proxy location'ları `Connection $connection_upgrade` kullanıyor. SignalR'ın long-polling fallback'i de sağlamlaştı. | ✅ |
| ~~**D5**~~ | ~~**Eksik 80 → 443 yönlendirme blokları**~~ | → ✅ **2026-08-15'te sunucuda uygulandı.** `portal.altinkosenadirkuyumculuk.com` ve `yavuzsoylu.com` için açık redirect blokları eklendi. Öncesinde kazara çalışıyorlardı (default server'a düşüp `$host` sayesinde doğru adrese gidiyorlardı). | ✅ |
| **D6** | **Container'larda resource limit ve healthcheck yok** | 4 çekirdek / 6 GB makinede `app` + `portfolio` + `nginx` + (ayrı projeden) `eventmoments_*` birlikte çalışıyor. `restart: always` var ama healthcheck olmadığı için "ayakta ama cevap vermiyor" durumu yakalanmaz. | 🟡 Orta |
| **D9** | 🔴 **`index.html` önbellek başlığı taşımıyordu → kullanıcılar ESKİ uygulamayı çalıştırıyordu.** `Cache-Control` verilmediğinde tarayıcılar **sezgisel önbellekleme** uygular: dosyanın yaşının ~%10'u kadar süre sunucuya hiç sormadan servis ederler. `index.html` hash'li bundle adını (`main-XXXXXXXX.js`) içerdiği için, deploy'dan sonra tarayıcı eski `index.html`'i → eski bundle'ı → **eski uygulamayı** çalıştırmaya devam ediyordu; ekranda "Veri bekleniyor" kalıyordu. Mağaza TV'sindeki olayın ve "giriş yapınca veri gelmiyor, yenileyince geliyor" şikâyetinin ortak kök nedeni budur. **Çözüm:** `location = /index.html` → `no-cache, must-revalidate` (ETag var, revalidation 304 ile ucuz); hash'li varlıklar → `max-age=31536000, immutable`, kalıp: `location ~* "-[A-Za-z0-9]{8}\.(?:js|css)$"`. ⚠️ **Regex ÇİFT TIRNAK içinde olmalı:** nginx'te `{` ve `}` blok ayracıdır, tırnaksız yazılırsa `unknown directive` hatasıyla açılmaz. Kalıp bilinçli olarak dar tutuldu — `assets/` altındaki hash'siz görseller bu kurala düşmez, yoksa logo değiştirildiğinde bir yıl eski görsel servis edilirdi. ⚠️ Aynı zamanda `location /uploads/` **`^~` yapılmalı** — regex location'lar prefix'lerin önüne geçtiği için aksi halde logolar varlık kuralına düşer ve `alias` bozulur. Ayrıca bu düzeltme, web tarafındaki **otomatik sürüm yenilemenin ön koşuludur** (`SarfiyumUI/AGENTS.md` §9.1): `index.html` önbelleklenirse kontrol asla tetiklenmez. | 🔴 Her deploy'da yaşanıyordu |
| **D7** | **Public dashboard'ın ön koşulu: rate limiting** | Anonim uç noktalar (`/publichub`, `/api/public/`) için `limit_conn_zone` + `limit_req_zone` gerekir. Girişli tarafta JWT bir fren; public'te hiç fren yok. **Kodda yapılabilecek korumalar yapıldı** (geçersiz slug anında reddediliyor ve **geçersiz slug'lar da 1 dk önbelleğe alınıyor**, böylece bot denemesi DB'ye inmiyor; public grup için tenant başına tek serileştirme yapılıyor). Kalan iş nginx tarafında. **Önerilen değerler ve gerekçesi §5.4'te.** | 🟠 Adım 7 bağımlılığı |
| **D8** | 🔴 **`nginx.conf`'ta `/publichub` location bloğu YOK — public ekran canlıda ÇALIŞMAZ.** | Mobil uygulama `https://sarfiyum.com/publichub` adresine bağlanıyor, ancak nginx'te yalnızca `/uploads/`, `/`, `/api/` ve `/goldhub` blokları var. `/publichub` isteği `location /`'a düşüyor ve `try_files ... /index.html` ile Angular'a yönleniyor; **WebSocket backend'e hiç ulaşmıyor.** Rate limiting'den önce bu blok eklenmelidir. Yapılandırma §5.4'te. | 🔴 **Public için engelleyici** |

### 5.4 Public ekran için nginx yapılandırması (YAPILMADI)

`http { }` bloğuna (mevcut `map` satırının yanına):

```nginx
    limit_conn_zone $binary_remote_addr zone=publichub_conn:10m;
    limit_req_zone  $binary_remote_addr zone=public_api:10m rate=30r/m;
    limit_conn_status 429;
    limit_req_status  429;
```

`sarfiyum.com` server bloğuna (mobil uygulama buraya bağlanıyor):

```nginx
        # D8 — BU BLOK OLMADAN PUBLIC EKRAN ÇALIŞMAZ.
        location /publichub {
            limit_conn publichub_conn 20;
            set $sarfiyum_pubhub sarfiyum_backend:8080;
            proxy_pass http://$sarfiyum_pubhub;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection $connection_upgrade;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_read_timeout 3600;
            proxy_send_timeout 3600;
        }

        # /api/ bloğundan daha uzun önek olduğu için nginx bunu seçer.
        location /api/public/ {
            limit_req zone=public_api burst=20 nodelay;
            set $sarfiyum_pubapi sarfiyum_backend:8080;
            proxy_pass http://$sarfiyum_pubapi;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_read_timeout 90;
            proxy_connect_timeout 90;
            proxy_send_timeout 90;
        }
```

**Limit değerlerinin gerekçesi:**

- `limit_conn 20` — daha sıkı OLMAMALI: **Türk mobil operatörleri CGNAT kullanıyor**, yani
  aynı baz istasyonundaki onlarca müşteri **aynı IP'den** görünebilir. 5 gibi bir limit
  gerçek kullanıcıları keserdi. 20, tek bir kötü niyetli hostu durdurur ama normal
  kullanımı engellemez.
- `rate=30r/m, burst=20` — tenant bilgisi uygulama açılışında **bir kez** çağrılıyor;
  dakikada 30 fazlasıyla bol, burst yeniden açılışları tolere eder.

Limitler çok sıkı gelirse nginx error log'unda `limiting connections` / `limiting requests`
satırları görülür; değerler trafiğe bakılarak ayarlanmalıdır.

### 5.3 Takip işleri (sunucu değişikliği sonrası)

- **`deploy.yml` sadeleştirmesi:** D3 çözüldüğü için nginx'in her deploy'da yeniden başlatılmasına gerek kalmadı. `docker restart sarfiyum_nginx` → `docker exec sarfiyum_nginx nginx -s reload` yapılabilir veya tamamen kaldırılabilir. Bu, deploy sırasında **portfolio ve eventmoments dahil tüm sitelerdeki** kısa kesintiyi ortadan kaldırır. Henüz yapılmadı — repoyu (workflow dosyasını) değiştireceği için ayrı ele alınacak.
- **D8** (eksik `/publichub` bloğu — public için **engelleyici**), **D7** (rate limiting) ve **D6** (healthcheck + resource limit) hâlâ açık. D8 ve D7 için hazır yapılandırma §5.4'te.

---

## 6. YEREL ÇALIŞTIRMA

```bash
# API (Development → Mock fiyat servisi devreye girer)
cd SarfiyumAPI
dotnet restore
dotnet run --project SarfiyumAPI            # https://localhost:7140, Swagger: /swagger

# Migration
dotnet ef migrations add <Ad> -p SarfiyumAPI.Data -s SarfiyumAPI
dotnet ef database update  -p SarfiyumAPI.Data -s SarfiyumAPI
```

> `appsettings.json` içindeki connection string **canlı sunucuya** bakar. Lokal geliştirme için yorumdaki SQLEXPRESS satırına geçilmelidir; aksi halde lokal testler canlı veriyi bozar. **Bu bir tuzaktır, dikkat.**

---

## 7. BULGULAR — GÜVENLİK (aksiyon alınmadı, karar bekliyor)

| # | Bulgu | Konum | Şiddet |
|---|---|---|---|
| G1 | **SQL Server `sa` şifresi düz metin ve git geçmişinde** | `SarfiyumAPI/appsettings.json` | 🔴 Kritik |
| G2 | **JWT imzalama anahtarı düz metin ve git geçmişinde** (32 karakterlik zayıf anahtar) | `appsettings.json → Jwt:Key` | 🔴 Kritik |
| G3 | **Datshop canlı socket key'i düz metin** ("git'te görünmesin" yorumu var ama key yazılı) | `appsettings.json → SocketServer:Key` | 🔴 Kritik |
| G4 | **Admin kullanıcı adı + şifresi kaynak kodda** (`yvzsylu` / düz metin), seed data içinde | `AppDbContext.OnModelCreating` | 🔴 Kritik |
| G5 | Non-viewer JWT ömrü **6 ay**, iptal mekanizması yok. Çalınan token 6 ay geçerli (tek fren: `SessionCheckMiddleware`) | `AuthService.LoginAsync` | 🟠 Yüksek |
| G6 | Şifre üretimi `System.Random` ile (kriptografik değil, tahmin edilebilir) | `AuthService.GenerateRandomPassword` | 🟠 Yüksek |
| ~~G7~~ | ~~`ToggleWeb` / `ToggleMobile` metotlarında `tenantId` null kontrolü yok~~ → ✅ **2026-08-15'te çözüldü.** Her ikisine de `if (string.IsNullOrEmpty(tenantId)) return Unauthorized();` eklendi; derleyicinin CS8604 uyarıları da kapandı. | `ProductController` | ✅ |
| G8 | `ValidateAudience = false` ama token'a audience yazılıyor; `Jwt:ExpiryInMinutes` ayarı tanımlı ama hiç kullanılmıyor | `Program.cs`, `AuthService` | 🟡 Orta |
| G9 | `AddPolicy("AdminOnly"/"UserOnly")` tanımlı, hiçbir yerde kullanılmıyor (ölü yapılandırma) | `Program.cs` | 🟢 Düşük |
| ~~G10~~ | ~~**Logout endpoint'i yok.**~~ → ✅ **2026-08-15'te çözüldü (Adım 3).** `POST /api/auth/logout` eklendi (`[Authorize]`, gövde almaz — kimin hangi oturumu kapattığı token claim'lerinden okunur, böylece bir istemci başkasının oturumunu kapatamaz). Yaptığı iş: `UserActiveSession` satırını siler, oturum önbelleğini düşürür, `ForceCheckSession` sinyali yollar ve kullanıcının açık SignalR bağlantılarını yayın gruplarından çıkarır. **Token'daki `SessionId` DB'dekiyle karşılaştırılır:** eski bir token, aradan yapılmış yeni bir girişin oturumunu kapatamaz. İşlem idempotenttir. Web ve mobil istemciler bağlandı. | `AuthController`, `AuthService` | ✅ |

> **Not:** G1–G4 için sadece dosyayı temizlemek yetmez; **anahtarların rotasyonu + git geçmişinin temizlenmesi** gerekir.

### Repo görünürlüğü (2026-08-15'te doğrulandı)

| Repo | Görünürlük | Sonuç |
|---|---|---|
| `SarfiyumAPI` | **private** | G1–G4 sırları internete açık **değil**. Risk, repo erişimi olanlar + `GH_PAT` sızıntısı ile sınırlı. Yine de rotasyon şart, ama "acil müdahale" değil **planlı bakım** işi. |
| `SarfiyumUI` | **private** | — |
| `sarfiyum_mobile` | 🔴 **PUBLIC** | Kaynak kod herkese açık. **Sır taraması yapıldı: sızıntı yok** — `key.properties`, `*.jks`, `*.keystore` `android/.gitignore`'da, takip edilen dosyalarda anahtar/parola yok. Açığa çıkan tek şey kaynak kodun kendisi ve `https://sarfiyum.com` uç noktaları (zaten APK'dan çıkarılabilir). |

> ⚠️ `sarfiyum_mobile`'ın public olması **bilinçli bir tercih mi?** Değilse private'a çekilmeli. Şu an bir güvenlik açığı yaratmıyor ama gelecekte oraya eklenecek bir anahtar doğrudan internete düşer — bu repo için "sır ekleme" riski kalıcı olarak yüksektir.

> **G1–G4 rotasyon sırası önemlidir:** `Jwt:Key` değiştiği an **tüm mevcut token'lar geçersiz olur** → web ve mobildeki herkes çıkış yapar (6 aylık `User` token'ları dahil). Bu bir **bakım penceresi** işidir, mesai içinde yapılmamalıdır.

---

## 8. BULGULAR — PERFORMANS (hub / broadcast hattı)

Sunucu 4 çekirdek / 6 GB RAM olduğu için bu maddeler doğrudan yaşanan yavaşlamanın kaynağıdır.

| # | Bulgu | Detay | Etki |
|---|---|---|---|
| ~~**P1**~~ | ~~**Yayın, gelen paket sayısına bağlı**~~ | → ✅ **2026-08-15'te çözüldü (Adım 4).** `price_changed` handler'ı **artık yayın yapmıyor**; yalnızca `_priceCache`'i güncelleyip bir "kirli" bayrağı bırakıyor. Yayını sabit aralıklı bir tick döngüsü (`RunBroadcastLoopAsync`, varsayılan **1000 ms**, `SocketServer:BroadcastIntervalMs`) yapıyor. Aradaki paketler doğal olarak birleştiriliyor (coalescing) — istemciye giden veri hep en güncel, sadece daha seyrek. **Yeni veri gelmediyse tick boşa dönüyor**, yani piyasa sakinken hiç yayın yapılmıyor. Watchdog ayrı bir döngüye alındı (periyotları farklı, biri diğerini bekletmemeli). | ✅ |
| ~~**P2**~~ | ~~**Delta yok, her seferinde tam liste**~~ | → ✅ **2026-08-15'te çözüldü (Adım 5).** Yayın snapshot + delta oldu. `IPriceFeedStateTracker` grup başına son gönderilen durumu tutuyor; yalnızca fiyatı oynayan satırlar `{id, bid, ask}` olarak gidiyor. Tam liste (`ReceivePriceSnapshot`) **bağlantı anında**, **ürün yapılandırması değişince** ve **60 saniyede bir** gönderiliyor. Hiçbir fiyat oynamadıysa **hiç mesaj gönderilmiyor**. | ✅ |
| **P3** | **Broadcast içinde DI scope + DB erişimi** | Her yayında `CreateScope()`, 3 servis çözümleme, cache miss'te DB sorgusu. ⚠️ **P1'den sonra önceliği düştü:** yayın frekansı paket frekansından koptuğu için bu maliyet artık saniyede birkaç kez değil, **saniyede en fazla bir kez** oluşuyor; cache TTL'leri de (10 dk / 1 sa) hizalandığından DB'ye nadiren iniliyor. `TenantSnapshot` refactor'ı hâlâ doğru yön ama artık acil değil. | 🟡 Orta (P1 sonrası düştü) |
| ~~**P4**~~ | ~~**`ShowOnWeb`/`ShowOnMobile` sunucuda filtrelenmiyor**~~ | → ✅ **2026-08-15'te çözüldü (Adım 5).** Filtre `PriceCalculator` içinde, `PricePlatform` parametresiyle uygulanıyor. Sonuç olarak yayın grupları **platform bazlı** oldu (`_Std_Web` / `_Std_Mobile`); gizli ürünlerin fiyatları artık tel üzerinden hiç gitmiyor — hem trafik hem gizlilik sızıntısı kapandı. Bu, tenant başına 1 yerine 2 serileştirme demek ama her biri daha küçük ve delta ile birlikte net kazanç büyük. | ✅ |
| ~~**P5**~~ | ~~**`SessionCheckMiddleware` her istekte DB'ye gidiyor**~~ | → ✅ **2026-08-15'te çözüldü (Adım 3).** Yeni `IActiveSessionCache` (5 dk mutlak süre) devreye girdi; middleware artık gereksiz `CreateScope()` de açmıyor (`RequestServices` zaten istek kapsamı). **Tek cihaz kuralı bozulmadı:** tablonun tek yazarı `AuthService` olduğu için login önbelleği YENİ `SessionId` ile günceller, logout ise düşürür — eski cihaz bir sonraki istekte yine anında 401 alır. "Oturum yok" bilgisi de önbelleğe alınır, böylece çıkmış ama istek göndermeye devam eden istemci DB'yi meşgul etmez. | ✅ |
| ~~**P6**~~ | ~~**`GoldHub.OnConnectedAsync` içinde 2 DB sorgusu**~~ | → ✅ **Kısmen çözüldü (Adım 3).** Oturum doğrulaması aynı `IActiveSessionCache` üzerinden yapılıyor; TV ekranlarının reconnect fırtınası artık DB'ye inmiyor. **Kalan:** `HasSpecialSettingsAsync` hâlâ bağlantı başına bir sorgu — düşük maliyetli (`AnyAsync`) olduğu için ertelendi. | 🟢 Kalanı düşük |
| **P7** | **Viewer için `Task.Delay(tokenÖmrü)` fire-and-forget** | Her viewer bağlantısı için bekleyen bir task. Çok viewer = çok bekleyen timer. | 🟡 Orta |
| ~~**P8**~~ | ~~**Bağlı olmayan viewer'lara boşa yayın**~~ | → ✅ **2026-08-15'te çözüldü (Adım 3).** `BroadcastToTenants` artık her özel viewer için `IHubConnectionRegistry.GetConnections(...)` kontrolü yapıyor; bağlantısı olmayan viewer için ne liste hesaplanıyor ne de gönderim yapılıyor. Atlanan sayı `[PERF]` logunda raporlanıyor. **Filtrenin doğru kaynağı budur:** `UserActiveSession` "oturum açık mı"yı tutar, "şu an bağlı mı"yı değil — eski online filtresinin çalışmamasının kök nedeni de buydu. Mock servise de aynı davranış verildi ki lokal ile canlı sapmasın. | ✅ |
| ~~**P9**~~ | ~~**Log gürültüsü**~~ | → ✅ **2026-08-15'te çözüldü (Adım 2).** Yayın hattındaki rutin loglar `Debug`'a indirildi: `[SOCKET_IN]`, başarılı `[PERF]`, `[SOCKET] Yeniden bağlanılıyor`, `GoldHub` gruplama kararı, Mock servisin tenant başına satırı. Tekrarlayan **uyarılar susturulmadı, throttle edildi**: `[OVERLAP]` artık her atlanan pakete satır yazmak yerine 5 dakikada bir toplam sayıyı yazıyor; `[PRICE_CONFIG]` ürün başına 5 dakikada bir loglanıyor. `Warning`/`Error` seviyesi yalnızca gerçek anomalilerde kalıyor. **Kalan iş: sunucu tarafı Docker log rotation (aşağıya bak).** | ✅ |
| **P9b** | **Docker log rotation yok** | → Sunucu tarafı bulgu olarak **§5.2 D1**'e taşındı (uygulanacak compose bloğu orada). Kod tarafı çözüldü, tavan hâlâ yok. | 🟠 Sunucu işi — §5.2 D1 |
| ~~**P10**~~ | ~~**Watchdog agresif**~~ | → ✅ **2026-08-15'te çözüldü (Adım 2).** Boşta kalma eşiği 15 sn → **60 sn** (`SocketServer:WatchdogIdleSeconds`), ardışık sıfırlamalarda **üstel geri çekilme** eklendi (eşik × 2^(n-1), üst sınır `SocketServer:WatchdogMaxBackoffSeconds` = 300 sn). Gerçek veri geldiği an seri sıfırlanır ve tek satırlık "veri akışı yeniden başladı" bilgisi loglanır. İlk sıfırlama `Warning`, ardışık olanlar `Debug` → piyasa kapalıyken gece boyunca log şişmesi ve karşı tarafta rate-limit riski ortadan kalktı. Piyasa saati tablosu **gerekmedi**; backoff aynı sorunu sabit kodlanmış saat varsayımı olmadan çözüyor. | ✅ |
| **P11** | **Her tick'te yeni nesne allocation'ı** | `CalculatePriceList` tenant × ürün kadar yeni `PriceData` üretiyor → GC baskısı. ⚠️ **P1 ile büyük ölçüde hafifledi:** allocation frekansı paket frekansından tick frekansına düştü. Ayrıca Adım 4'te viewer özel ayarları için ürün başına lineer arama (`FirstOrDefault`) yerine tek seferlik `Dictionary` indeksi kuruldu ve liste kapasitesi önden verildi. Kalan iyileştirme (nesne havuzu / önceden hesaplanmış liste yeniden kullanımı) P2/P3 ile birlikte anlamlı olur. | 🟢 Düşük (P1 sonrası) |
| **P12** | **Yatay ölçekleme imkânsız** | `IMemoryCache` + SignalR backplane yok. İkinci container açılırsa hem cache hem grup üyelikleri bölünür. Şu an tek container olduğu için sorun değil ama **büyüme tavanı burası**. | 🟡 Orta (mimari) |

### Önerilen yön
1. ~~**Tick tabanlı yayın**~~ → ✅ Adım 4'te yapıldı.
2. **Snapshot mimarisi:** Tenant konfigürasyonu (ürünler + çarpanlar + özel ayarlar) tek bir in-memory `TenantSnapshot` nesnesinde tutulur; DB'ye sadece invalidation olayında (`ClearCache` çağrılan yerler) gidilir. → P3. **Önceliği P1 sonrası düştü.**
3. **Sunucu tarafı platform filtresi:** `PriceData`'ya `ProductId` eklenip, `_Web` / `_Mobile` alt grupları veya iki ayrı önceden hesaplanmış liste. → P4 + istemci string eşleştirme kırılganlığı çözülür.
4. **Delta yayını:** İlk bağlantıda tam liste (`ReceiveSnapshot`), sonrasında sadece değişenler (`ReceiveDelta`). → P2 çözülür.
5. **Oturum durumunu cache'e almak / `ConnectionId` bazlı kick**, `SessionCheckMiddleware`'i DB'den kurtarmak. → P5 + P6.
6. **Logout endpoint + `UserActiveSession` temizliği** ve gerçek online tespiti için `GoldHub.OnDisconnectedAsync`. → G10 + P8.
7. ~~**Log seviyelerini `Debug`'a indirmek**~~ → ✅ Adım 2'de yapıldı. Docker log rotation (`max-size`, `max-file`) hâlâ sunucuda elle yapılmalı. → P9b.

### Log seviyeleri nasıl okunur? (Adım 2 sonrası)

Yayın hattının ayrıntılı akışı artık **varsayılan olarak yazılmaz**. Bir sorunu incelerken açmak için
`appsettings.json` → `Logging:LogLevel:Default` değerini `"Debug"` yapmak yeterlidir
(canlıda `Logging__LogLevel__Default` environment değişkeniyle geçici olarak açılabilir).

| Seviye | Ne görürsün |
|---|---|
| `Information` (varsayılan) | Servis başlangıcı, soket bağlantısı, kopma, veri akışının yeniden başlaması, yavaş yayın uyarısı, hatalı fiyat konfigürasyonu (throttle'lı) |
| `Debug` | Her paket (`[SOCKET_IN]`), her yayın süresi (`[PERF]`), watchdog'un ardışık denemeleri, gruplama kararları |

---

## 9. BULGULAR — MİMARİ / KOD KALİTESİ

| # | Bulgu |
|---|---|
| ~~M1~~ | ~~**Viewer grup üyeliği bağlantı anında sabitleniyor — canlı senkronizasyon yok.**~~ → ✅ **2026-08-15'te çözüldü.** `IHubConnectionRegistry` (UserId → ConnectionId) + `IViewerPricingNotifier` eklendi. `UpsertSettingAsync` viewer'ı standart gruptan çıkarır, `DeleteSettingAsync` (başka ayarı kalmadıysa) geri alır. `GoldHub.OnDisconnectedAsync` eklendi. Artık viewer bağlıyken ayar değişse de tek ve doğru fiyat listesini alır. |
| ~~M2~~ | ~~**`GetOnlineVisitorsWithSettingsAsync` 30 sn cache'li** → yeni bağlanan özel ayarlı viewer 30 saniyeye kadar hiç fiyat görmeyebilir.~~ → ✅ **2026-08-15'te çözüldü.** Çalışmayan online filtresi kaldırıldı; sonuç yalnızca `VisitorProductSetting` satırlarına bağlı ve bu satırlar Upsert/Delete'te `ClearCache` tetiklediği için cache 1 saate çıkarıldı. Kör nokta kalmadı. Metot adı `GetVisitorsWithSpecialSettingsAsync`, cache anahtarı `VisitorSpecialSettings_{tenantId}` olarak düzeltildi (eski adlar artık yanıltıcıydı). |
| ~~M3~~ | ~~**`CalculatePriceList` iki kez kopyalanmış ve davranışları farklı**~~ → ✅ **2026-08-15'te çözüldü (Adım 5).** Hesap tek bir `PriceCalculator`'a (Core arayüz + Service implementasyon, singleton) çıkarıldı. Mock'un "eşleşmeyen ürüne sahte 1000/1010 üret" davranışı hesabın içinden alınıp `BuildMarketDataWithFallback` ile **çağrı öncesine** taşındı — yani uydurma açıkça mock'a ait, hesap kuralı her iki ortamda birebir aynı. *Localde görünen ürünün canlıda görünmemesi* sorunu kalktı. |
| ~~M4~~ | ~~`finalBid > 0 ? finalBid : null` — çarpan/makas sonucu 0 veya negatif olursa fiyat sessizce `null` gider.~~ → ✅ **2026-08-15'te çözüldü.** Fiyat artık SADECE kaynak veri yoksa `null` gider; hesap sonucu ne çıkarsa o gönderilir. Pozitif olmayan sonuçta `[PRICE_CONFIG]` uyarısı loglanır (ürün adı, Id, TenantId, çarpanlar) — böylece yanlış konfigürasyon gizlenmek yerine görünür olur. |
| M5 | **`Symbol` / `SymbolCategory` ölü kodu.** Entity'ler DbContext'ten kaldırılmış ama `ISymbolService`, `ISymbolCategoryService`, `SymbolDto`, `CreateUpdateSymbolDto`, `SyncSymbolRequest`, `SymbolCategoryDto` hâlâ Core'da; DI kaydı ve implementasyon yok. **Angular'da `admin/symbols` ve `admin/symbol-categories` modülleri hâlâ duruyor ve olmayan endpoint'lere gidiyor.** |
| M6 | `SarfiyumAPI.Entity` projesi tamamen boş (`Class1.cs`) — solution'dan çıkarılabilir. |
| M7 | `EntityBase` denetim alanları (`CreatedAt`, `ModifiedAt`, `CreatedById`...) **otomatik doldurulmuyor** — `SaveChanges` override'ı yok. Bazı yerlerde elle set ediliyor, çoğu yerde boş. |
| M8 | **Soft delete tutarsız:** filtre sadece 4 entity'de. `VisitorProductSetting.DeleteSettingAsync` ve `ViewerAccountService.DeleteViewerAsync` **gerçek delete** yapıyor. |
| M9 | `IRepository<T>` `IQueryable` döndürüyor → servisler `Include`/`AnyAsync`/`MaxAsync` için doğrudan EF Core'a bağımlı. Soyutlama fayda sağlamıyor, sadece bir katman ekliyor. |
| M10 | `TenantController.CreateWithUser` **transaction kullanmıyor**; manuel rollback var ama `TenantService.DeleteAsync` soft-delete olduğu için seed edilmiş kategori/ürünler ortada kalır. |
| M11 | `DashboardService.MonthlyRevenue` **string'e göre sıralanıyor** (`"10/2025" < "2/2026"`) → grafik sırası yanlış. 6 aylık ödeme kaydı RAM'e çekilip gruplanıyor. |
| **M18** | **Seed kayıtları sabit `Id` taşımıyor → her migration katalog verisini siliyor.** `EntityBase.Id` varsayılanı `Guid.NewGuid()` olduğu için `SystemCatalog` seed'i her model derlemesinde yeni GUID'ler üretiyor; EF bunu "satırlar değişti" sanıp **66 satırı silip yeniden ekliyor.** Üretilen her migration'da mevcut (2026-08-15'teki `publicfeedsettings` migration'ı ham hâliyle **56 KB**'tı ve 132 `DeleteData` içeriyordu; el ile sadeleştirildi). Canlıda gereksiz veri churn'ü ve `dotnet ef`'in *"may result in the loss of data"* uyarısı demek. **Çözüm:** seed kayıtlarına sabit `Id` vermek. |
| M12 | `ExpirationJob` `TimeZoneInfo.Local` kullanıyor; container'da `TZ` set edilmemişse UTC'ye düşer → "gece 00:00" beklentisi 3 saat kayar. |
| M13 | Claim isimleri tutarsız: token'a `tenantId` (küçük t) yazılıyor, kodda `FindFirst("TenantId")` (büyük T) okunuyor. .NET claim araması case-insensitive olduğu için çalışıyor ama kırılgan. Aynı şekilde `Jwt:Key` vs `JWT:Key`. |
| ~~M14~~ | ~~`GoldHub` `OnDisconnectedAsync` override etmiyor~~ → ✅ **2026-08-15'te kapatıldı (Adım 3).** `OnDisconnectedAsync` bağlantıyı `IHubConnectionRegistry`'den düşürüyor ve **yapması gereken tek şey bu.** ⚠️ **Önceki not düzeltildi:** burada "kalan iş" olarak yazılan *"`UserActiveSession` tablosu da güncellenmeli"* maddesi **yanlış bir hedefti.** `UserActiveSession` "bu kullanıcının bu istemci tipindeki oturumu hangi cihazda" bilgisini tutar; hub bağlantısı ise sayfa yenilemede, ağ dalgalanmasında veya mobil uygulamanın arka plana atılmasında rutin olarak kopar. Bağlantı koptu diye satırı silmek **tek cihaz kuralını bozardı** — kullanıcı sayfayı yenilediğinde oturumu düşerdi. Satırın silinmesi gereken tek yer çıkıştır ve orası G10 ile çözüldü. |
| ~~M15~~ | ~~`VisitorSettingService` kullanılmayan `IRepository<UserActiveSession>` alıyor.~~ → ✅ **2026-08-15'te çözüldü.** Constructor'dan kaldırıldı. |
| ~~M16~~ | ~~Eski metot ~50 satır yorum olarak duruyor.~~ → ✅ **2026-08-15'te çözüldü.** Ölü yorum bloğu silindi. |
| M17 | Kategoriler tenant başına yaratılıyor ama **ekleme/silme API'si yok**, sadece isim güncelleme var. Sabit 5 kategori varsayımı hem backend seed'inde hem Angular dashboard'ında (`categoryIndex` 1–5 switch) gömülü. |

---

## 10. PLANLANAN İŞ — Public (Girişsiz) Fiyat Ekranı

**İstek:** Bir müşteri için mobil uygulamada ana fiyat ekranı **girişsiz (public)** açılacak. Giriş de korunacak:
- **Girişsiz kullanıcı** → tenant'ın kendi belirlediği **tenant multiplier** ile hesaplanmış **ana fiyatları** görür.
- **Giriş yapan Viewer** → bugünkü gibi **kendi özel fiyatlarını** görür.
- Bu değişiklik **sadece o müşterinin branch'inde** aktif olmalı; diğer müşterilerin uygulamaları etkilenmemeli.

### Backend'de gerekenler (tasarım notu — henüz uygulanmadı)

1. **Tenant'ta bir bayrak:** `Tenant` veya `TenantSetting` üzerine `IsPublicFeedEnabled` (bool). Public yayın sadece bu bayrak açık tenant'lar için üretilir. → Migration gerekir.
2. **Tenant'ın public olarak tanınması:** Mobil uygulama giriş yapmadan hangi tenant olduğunu söylemeli. İki seçenek:
   - **a)** Build-time sabit `PUBLIC_TENANT_ID` (branch başına `--dart-define`). Basit, ama tenant id'si APK içinde açığa çıkar.
   - **b)** Kısa, tahmin edilemez bir `PublicSlug` (örn. `cilcilaltin`) → `GET /api/public/tenant/{slug}` ile tenant çözümlenir. Daha temiz, önerilen.
3. **Anonim SignalR erişimi:** `GoldHub` şu an sınıf seviyesinde `[Authorize]`. İki yol var:
   - **Ayrı hub (`PublicPriceHub`)** — `[AllowAnonymous]`, sadece `{tenantId}_Public` grubuna abone eder. **Önerilen:** mevcut hub'ın oturum/kick mantığına hiç dokunmadan izole kalır.
   - Mevcut hub'ı `[AllowAnonymous]` yapıp içeride ayrıştırmak — mevcut güvenlik akışını riske atar, **önerilmez**.
4. **Public yayın listesi:** `BroadcastToTenants` içinde, bayrağı açık tenant'lar için ek bir liste hesaplanır ve `{tenantId}_Public` grubuna gönderilir. Bu liste:
   - `ShowOnMobile = true` olan ürünlerle sınırlı olmalı (P4 çözülürse doğal olarak gelir),
   - tenant'ın standart çarpanlarını kullanır (viewer override'ı **yok**),
   - "public'te gizle" ihtiyacı doğarsa `TenantProduct.ShowOnPublic` gerekebilir — kullanıcıyla netleştirilmeli.
5. **Rate limiting / abuse koruması:** Anonim endpoint açıldığı an bot trafiğine açılır. En azından IP başına bağlantı limiti ve `MapHub` üzerinde bağlantı sayısı sınırı düşünülmeli.
6. **Abonelik kontrolü:** Tenant'ın aboneliği bittiğinde public yayın da durmalı (`GetActiveTenantIdsAsync` zaten `IsActive` filtreliyor, ama `SubscriptionEndDate` kontrolü **login'de** yapılıyor, yayında yapılmıyor — public açılırsa bu bir gelir kaçağı olur). ⚠️ Bunu ayrıca not et.

> Mobil taraftaki akış ve ekran tasarımı `sarfiyum_mobile/AGENTS.md` §8'de.

---

## 10.4 GENEL ÖZET — Public dashboard öncesi sağlamlaştırma (2026-08-15)

> **Amaç:** Bir müşterinin mobil uygulamasında fiyat ekranını **girişsiz (public)** açmadan
> önce, üç repoda biriken hata ve performans borcunu temizlemek. Public açıldığında bağlantı
> sayısı ve dolayısıyla sunucu yükü artacağı için, önce hattın sağlam ve ucuz olması gerekiyordu.
>
> **Durum: Adım 1–6 tamamlandı, HİÇBİRİ COMMIT EDİLMEDİ.** Adım 7 (public dashboard) için
> tüm teknik ön koşullar hazır.

### Tek bakışta

| # | Adım | Kapsam | Durum |
|---|---|---|---|
| 1 | Görünür bug'lar | Viewer grup senkronizasyonu, sessiz `null` fiyat, alış oku | ✅ |
| 2 | Log gürültüsü & watchdog | Disk şişmesi, gece boyu reconnect fırtınası | ✅ |
| 2.5 | Sunucu yapılandırması | nginx + docker-compose (kullanıcı uyguladı) | ✅ |
| 3 | Oturum & hub hattı | Logout endpoint'i, oturum önbelleği, boşa yayın | ✅ |
| 4 | Yayın performansı | Tick tabanlı yayın | ✅ |
| 5 | API sözleşmesi + delta | `id`/`sourceKey`, sunucu tarafı filtre, snapshot+delta | ✅ |
| 6 | İstemci performansı | Web `OnPush`, mobil `Consumer` + gruplama | ✅ |
| **7** | **Public dashboard** | **Asıl hedef** | 🎯 **Sıradaki** |

### Kapatılan bulgular (33)

| Repo | Bulgular |
|---|---|
| **API** | M1, M2, M3, M4, M14, M15, M16 · P1, P2, P4, P5, P8, P9, P10 · G7, G10 |
| **Sunucu** | D1, D2, D3, D4, D5 |
| **Web** | W1, W2, W3, W4, W5, W8, W13, W23, W24 |
| **Mobil** | M1, M2, M3, M5, M6, M7, M13, M15, M-G3 · M4 (büyük ölçüde) |

Kısmi kalanlar: **P6** (hub'daki `HasSpecialSettingsAsync` sorgusu — ucuz, ertelendi),
**P11** (allocation frekansı düştü, nesne havuzu yapılmadı), **M4** (kart bazlı `Selector`).

### Sistemin davranışında ne değişti

**Fiyat yayını artık gelen paket frekansına bağlı değil.** Datshop paketleri yalnızca
önbelleği güncelliyor; sabit aralıklı bir tick döngüsü (1 sn) son durumu yayınlıyor.
Yeni veri yoksa **hiç yayın yapılmıyor**.

**Tam liste yerine delta gidiyor.** Bağlantı anında, ürün yapılandırması değişince ve
60 saniyede bir tam liste; aradaki tick'lerde yalnızca fiyatı oynayan satırlar.

**Gizli ürünler tel üzerinden hiç gitmiyor.** `ShowOnWeb`/`ShowOnMobile` filtresi sunucuda
uygulanıyor; bu yüzden yayın grupları platform bazlı oldu.

**Ürünlerin kalıcı kimliği var.** İstemciler `KATEGORİADI_ÜRÜNADI` string anahtarı yerine
`id` kullanıyor. Ürün adı değişince ayarlar artık sıfırlanmıyor.

**Satır boyutu ~%60 küçüldü.** `description` (ürün adının kopyası), `source` (sabit yazı) ve
satır başına `timestamp` kaldırıldı; fiyatlar 4 haneye yuvarlanıyor.

**Çıkış artık sunucuda da yapılıyor.** `POST /api/auth/logout` oturum satırını siliyor ve
bağlantıyı yayın gruplarından çıkarıyor — çıkış yapılmış bir TV ekranı artık fiyat almıyor.

**Oturum doğrulaması DB'ye gitmiyor.** Her API isteğinde ve her hub bağlantısında yapılan
sorgu önbelleğe alındı; tek cihaz kuralının anındalığı korundu.

### ⚠️ Bu bir KIRICI değişiklik

Backend artık `ReceivePrices` göndermiyor. Yeni sözleşme `ReceivePriceSnapshot` +
`ReceivePriceDelta`.

| Taraf | Durum |
|---|---|
| **Web** | Sorun yok — API ile aynı container'da deploy oluyor, birlikte gidiyorlar |
| **Mobil** | Sahadaki **üç uygulamanın** mevcut sürümleri yeni backend ile fiyat gösteremez. Yeni sürüm de eski backend ile çalışmaz. Store sürümü **backend deploy'undan sonra** çıkmalı |

### Test edilmesi gerekenler

- [ ] Dashboard açılışında fiyatlar **anında** geliyor mu (bağlantı anındaki snapshot)
- [ ] **Viewer'a özel fiyat tanımla → sil.** Delta mimarisinin en kırılgan noktası; ekran titremeden doğru listeye geçmeli
- [ ] Ürün adını değiştir — satır ikinci kez eklenmiyor, isim güncelleniyor mu
- [ ] Bir ürünü web'de gizle, mobilde açık bırak — iki istemci farklı liste görüyor mu
- [ ] Çıkış yaptıktan sonra TV ekranının fiyat akışı gerçekten kesiliyor mu
- [ ] Web ve mobilde aynı anda giriş — biri diğerini düşürmüyor, çıkışta yalnızca kendi oturumu kapanıyor
- [ ] 1 MB üstü firma logosu yükleme (sunucu tarafı D2)
- [ ] Piyasa kapalıyken loglara bak — watchdog artık dakikada birkaç kez reconnect denememeli

### Commit edilmemiş değişiklikler

| Repo | Değişen | Yeni |
|---|---|---|
| `SarfiyumAPI` | 14 dosya | 10 dosya (`PriceCalculator`, `PriceFeedStateTracker`, `ActiveSessionCache`, `HubConnectionRegistry`, `ViewerPricingNotifier` + arayüzler/DTO'lar) |
| `SarfiyumUI` | 5 dosya | — |
| `sarfiyum_mobile` | 6 dosya | — |

Migration / şema değişikliği **yok**, canlı DB'ye dokunulmadı.

### Abonelik kontrolü artık yayında da yapılıyor

`GetActiveTenantIdsAsync` eskiden yalnızca `IsActive && !IsDeleted` filtreliyordu; abonelik
kontrolü **sadece login'deydi**. Yani aboneliği biten bir tenant yeni giriş yapamıyor ama
halihazırda açık olan ekranları (mağaza TV'si, uygulaması açık kalan viewer'lar) fiyat almaya
devam ediyordu. Girişsiz public yayın açıldığında bu doğrudan bir **gelir kaçağına** dönüşürdü —
orada login diye bir fren hiç yok.

Filtreye `SubscriptionEndDate.HasValue && SubscriptionEndDate >= UtcNow` eklendi; karşılaştırma
`AuthService.LoginAsync`'dekiyle birebir aynı.

> ⚠️ **Bunun zincirleme bir sonucu vardı:** aktif tenant listesi 10 dakika önbelleklenmiş
> durumda ve `SubscriptionService` bu önbelleği **hiç temizlemiyordu**. Abonelik filtreye
> girmeden önce sorun değildi; girdikten sonra, aboneliği yenilenen bir tenant 10 dakikaya
> kadar yayına dönemezdi. `SubscriptionService`'in dört yazma noktasına (`AddSubscription`,
> `AdjustDays`, `DeleteSubscription`, `UpdateSubscription`) `ClearTenantCache()` eklendi.
> Dairesel bağımlılık kontrol edildi: `TenantService`, `ISubscriptionService`'e bağımlı değil.

**Kalan gecikme:** abonelik bittiği anda değil, en fazla 10 dakika içinde yayın durur
(önbellek ömrü). İş açısından kabul edilebilir; login kontrolü zaten anında.

### Üç repo senkronizasyon doğrulaması (2026-08-15)

Adım 5'teki sözleşme değişikliği sonrası üç taraf satır satır karşılaştırıldı.

| Kontrol | Sonuç |
|---|---|
| Event adları (`ReceivePriceSnapshot`, `ReceivePriceDelta`, `ForceCheckSession`) | ✅ Üçünde de birebir aynı |
| JSON alan adları (`id`, `symbol`, `sourceKey`, `category`, `categoryIndex`, `bid`, `ask`, `orderIndex`, `ts`, `seq`, `items`, `changed`) | ✅ Tümü `[JsonPropertyName]` ile sabitlenmiş; istemciler aynı adları okuyor |
| `ClientSource` → `PricePlatform` eşlemesi (Web=1, Mobile=2) | ✅ Grup adları tutuyor |
| Dinleyicilerin `start()` öncesi kaydı | ✅ Her iki istemcide de — bağlantı anındaki snapshot kaçmıyor |
| Snapshot/`seq` kuralları | ✅ İkisi de snapshot'sız delta uygulamıyor, eski `seq`'i yok sayıyor |

**Doğrulamada iki gerçek fark bulundu ve giderildi:**

1. **Mobilde `onreconnecting` yoktu.** Web yeniden bağlanırken yerel listeyi sıfırlıyor, mobil
   sıfırlamıyordu. Kopukluk sonrası snapshot'tan ÖNCE bir delta ulaşırsa mobil onu bayat listeye
   uygulayabilirdi. `onreconnecting` + `onreconnected` eklendi, `onclose` da durumu sıfırlıyor.
2. **Delta'da `null` fiyat davranışı farklıydı.** Web `null`'ı olduğu gibi yazıyor, mobil
   `?? item.bid` ile **eski fiyatı koruyordu** — ekranda bayat bir altın fiyatı kalabilirdi.
   Mobil, snapshot ayrıştırmasıyla tutarlı olacak şekilde `?? 0`'a çevrildi.

Ayrıca iki ölü referans temizlendi: web'de kullanılmayan `SettingsService` injection'ı ve
mobilde no-op'a düşmüş `loadProductConfigurations()` çağrısı (açılışta bir HTTP isteği daha az).

### Sıra dışı bekleyen işler

- **Mobil müşteri branch'lerine cherry-pick:** `client/cilcil-altin` ve `cilent/altinkose`
  hâlâ M1, M15, M-G3 ve Adım 5–6'nın tamamını beklemiyor.
- **`deploy.yml` sadeleştirmesi:** D3 çözüldüğü için nginx'in her deploy'da yeniden
  başlatılmasına gerek kalmadı (§5.3).
- **D6** (healthcheck/limit) ve **D7** (rate limiting — public'in ön koşulu).

### Adım 7 için hazır olan altyapı

| Gereken | Durum |
|---|---|
| Ürünlerin kalıcı kimliği | ✅ Adım 5 |
| Sunucu tarafı görünürlük filtresi | ✅ Adım 5 |
| Platform bazlı yayın grupları | ✅ Adım 5 |
| `PricePlatform.Public` enum + filtre kuralı | ✅ Adım 5 (yayına eklenmedi) |
| Delta ile düşük bant genişliği | ✅ Adım 5 |
| Tenant bayrağı (`IsPublicFeedEnabled`) | ❌ Migration gerekir |
| `[AllowAnonymous]` `PublicPriceHub` | ❌ |
| `GET /api/public/tenant/{slug}` | ❌ |
| nginx rate limiting (D7) | ❌ |
| Aboneliği biten tenant'ın yayınının durması | ✅ **2026-08-15'te çözüldü** — aşağıya bak |

**Başlamadan önce §11'deki sorular cevaplanmalı** — özellikle hangi müşteri, tenant kimliğinin
nasıl çözüleceği ve ayrı bir `ShowOnPublic` bayrağı gerekip gerekmediği. Son ikisi migration
ve üç repoda UI demek, sonradan dönmek maliyetli.

---

## 10.5 YOL HARİTASI — Adımlar

> **Nihai hedef:** bir müşteri branch'inde **public (girişsiz) dashboard** ekranı açmak (§10).
> Kullanıcının kararı: oraya geçmeden önce mevcut hataları temizlemek ve performansı düzeltmek.
> Bu tablo **kalıcıdır**; her adım bitince buradaki durum ve §12 günlüğü güncellenir.

| Adım | Kapsam | Bulgular | Durum |
|---|---|---|---|
| **1** | Görünür bug'lar | M1, M2, M4, G7, M15, M16 (API) · M1 (mobil) | ✅ Bitti — *commit edilmedi* |
| **2** | Log gürültüsü & watchdog | P9, P10 | ✅ Bitti — *commit edilmedi* |
| **2.5** | **Sunucu yapılandırması** (kod değil, `/opt/sarfiyum`) | D1, D2, D3, D4, D5 | ✅ **2026-08-15'te kullanıcı tarafından uygulandı.** Açık kalan: D6, D7 + `deploy.yml` sadeleştirmesi (§5.3) |
| **3** | Oturum & hub hattı | G10 (logout endpoint), P8 (bağlı olmayana boşa yayın), M14, P5, P6 · mobil M-G3 | ✅ Bitti — *commit edilmedi* |
| **4** | Yayın performansı | **P1 ✅** (tick tabanlı yayın), P11 ✅ (büyük ölçüde) | ✅ Bitti — *commit edilmedi*. **P2 → Adım 5'e taşındı**, **P3 → önceliği düştü** |
| **5** | **API sözleşmesi + delta** | P2, P4, M3 · web W5, W13 · mobil M2, M3, M6, M7, M13 | ✅ Bitti — *commit edilmedi*. **Üç repoda birden kod değişti** |
| **6** | İstemci performansı | web W1–W4, W8 · mobil M4, M5 | ✅ Bitti — *commit edilmedi* |
| **7** | **Public (girişsiz) dashboard** | §4.4 · `sarfiyum_mobile/AGENTS.md` §8 · **D8**, D7 | ✅ **Kod BİTTİ** (backend + mobil, `cilent/altinkose`). Kalan **sunucu işleri**: **D8 `/publichub` nginx bloğu (engelleyici)**, D7 rate limiting, migration uygulama, tenant'ta bayrak/slug set etme |
| **8** | Güvenlik borcu (bakım penceresi) | G1–G6, G8 · web W14, W15 · mobil M-G1 | ⏳ Ayrı planlanır |
| **9** | Ölü kod temizliği | M3, M5, M6 · web W9–W11, W6, W7 · mobil M10, M12 | ⏳ Düşük öncelik |

**Adım sırası dışındaki bekleyen işler:**
- Mobil M1 düzeltmesinin `client/cilcil-altin` ve `cilent/altinkose` branch'lerine cherry-pick'i (store'a çıkmadan önce şart).
- Adım 1 + 2 kodunun commit'i — `master` push = **canlıya deploy**, kullanıcı onayı gerekir.

---

## 11. AÇIK SORULAR (kullanıcıya sorulacak)

1. Public ekranda **hangi kategoriler/ürünler** görünecek? `ShowOnMobile` yeterli mi, yoksa ayrı `ShowOnPublic` mi gerekli?
2. Public ekran **hangi tenant** için? Slug tabanlı mı, build-time sabit mi?
3. Aboneliği biten tenant'ın public yayını durmalı mı? (Evet olmalı ama teyit gerekiyor.)
4. `SystemCatalog` ile Datshop'un gerçek sembol listesi senkron mu? (`5 Gr Gram Altın` gibi boşluklu `SourceKey`'ler var — Datshop bunları gerçekten böyle mi gönderiyor?)
5. Kategori ekleme/silme özelliği istenecek mi? (Şu an sabit 5 kategori varsayımı üç projeye de gömülü.)

---

## 12. DEĞİŞİKLİK GÜNLÜĞÜ

| Tarih | Değişiklik |
|---|---|
| 2026-08-14 | İlk oluşturma. Üç repo derinlemesine incelendi; mevcut özellikler, mimari, güvenlik/performans/kod kalitesi bulguları ve public fiyat ekranı tasarım notu kaydedildi. Kod değişikliği yapılmadı. |
| 2026-08-15 | Repo görünürlüğü doğrulandı (§7): `SarfiyumAPI` ve `SarfiyumUI` private, `sarfiyum_mobile` **public** (sır sızıntısı yok). G1–G4'ün şiddeti "acil" yerine **planlı bakım** olarak yeniden değerlendirildi; JWT rotasyonunun tüm oturumları düşüreceği notu eklendi. |
| 2026-08-15 | **Test altyapısı — mock artık canlıyı birebir taklit ediyor.** Yayın mantığı `IPriceBroadcaster` ve `PriceFeedHostedServiceBase` içine çıkarıldı; gerçek ve mock servisler artık **aynı koddan** geçiyor, yalnızca veri kaynağı bakımından ayrılıyorlar. Bu, M3'ün kök nedenini kalıcı olarak kapatır: Datshop yalnızca canlı sunucuyu whitelist'e aldığı için lokal test tek doğrulama imkânıdır ve mock ile canlının ayrışması test edilen şeyin canlıda çalışacak şey olmaması demekti. Mock artık sembolleri `SystemCatalog`'dan okuyor (66 sembolün tamamı gerçekçi başlangıç fiyatlarıyla), Datshop gibi 400 ms'de bir kısmi paket üretiyor ve `MockFeed` ayarlarıyla yönlendirilebiliyor (`PacketIntervalMs`, `Paused` = piyasa kapalı senaryosu, `Volatility`). Ayrıca **`DevDataSeeder`** eklendi (yalnızca Development, idempotent): test firması, `kuyumcu`/`musteri1`/`musteri2` kullanıcıları, 1 yıllık abonelik ve açık public yayın. `appsettings.Development.json` connection string'i **lokal SQLEXPRESS**'e çeviriyor — canlı DB'ye yanlışlıkla bağlanma tuzağı (§6) kapandı. **§13'e kapsamlı test planı** eklendi: 9 senaryo grubu (A–I) + lokalde test edilemeyenler listesi (L1–L14). Mobil senaryolar `sarfiyum_mobile/AGENTS.md` §10'da. |
| 2026-08-15 | **🔴 D9 — "Veri bekleniyor" sorununun kök nedeni bulundu: `index.html` önbelleklemesi.** Kullanıcı `oktayelbir` hesabıyla "giriş yapınca veri gelmiyor, yenileyince geliyor" durumunu bildirdi. **Önce sunucu elendi:** token alınıp `/goldhub`'a ham WebSocket ile bağlanıldı — bağlantı anında snapshot geldi (`_Std_Web`, 24 kayıt), yani backend doğru çalışıyor. **Sonra istemci elendi:** Playwright ile canlıda iki senaryo denendi (temiz giriş; çıkış→tekrar giriş) — **ikisi de sorunsuz geçti**, 22 satır göründü, "Veri bekleniyor" çıkmadı. Kalan tek değişken tarayıcı önbelleğiydi: canlıda `index.html` **hiçbir `Cache-Control` başlığı taşımıyor**, dolayısıyla tarayıcılar sezgisel önbellekleme uygulayıp eski kopyayı (ve eski bundle'ı) servis ediyor. Mağaza TV'sindeki olayın da aynı kök nedeni. nginx düzeltmesi §5.2 D9'da; web tarafına ayrıca otomatik sürüm yenileme eklendi (`SarfiyumUI/AGENTS.md` §9.1). |
| 2026-08-15 | **CANLIYA ALINDI.** DB yedeği alındı (`RESTORE VERIFYONLY` ile doğrulandı), `publicfeedsettings` migration'ı canlıya uygulandı (veri sayıları değişmedi: 13 tenant / 66 katalog / 327 ürün / 60 kullanıcı), API ve Web `master`'a push edildi, nginx kullanıcı tarafından güncellendi. `ALTINKÖŞE NADİR KUYUMCULUK` tenant'ı için `IsPublicFeedEnabled = 1`, `PublicSlug = 'altinkose'` set edildi ve uçtan uca doğrulandı. **Canlıda çıkan iki düzeltme:** (1) *Sıfır çarpan* — kuyumcu bir üründe kasıtlı olarak fiyat vermediğinde (Kredi Kartı kategorisi başta olmak üzere) çarpan 0 oluyor; Adım 1'de kaldırılan kural yüzünden bunlar web'de "0,00" gösterecekti. Artık çarpan 0 ise `null` gidiyor (hücre boş), yalnızca **negatif** sonuç hata sayılıp loglanıyor. (2) *`PublicController` sarmalayıcısı* — DTO çıplak dönüyordu, mobil `BaseApiService` `data` alanı bulamadığı için public ekran boş kalıyordu; `ServiceResult` ile sarmalandı. **Not:** piyasa kapalıyken fiyatların sabit görünmesi beklenen davranıştır (yeni veri yoksa yayın yapılmaz — A6). |
| 2026-08-15 | **Adım 7 — Public fiyat ekranı MOBİL tarafı tamamlandı.** Ayrı ekran yazılmadı: mevcut `UserDashboard` public modda çalışıyor, **tasarım birebir korundu** (kullanıcı isteği). Bayrak `main` ve `client/cilcil-altin`'de kapalı, yalnızca `cilent/altinkose`'de açık; branch farkı tek satır. `lib/` altındaki kod farkı: cilcil ↔ main **sıfır**, altinkose ↔ main **2 satır**. **Canlıya çıkmadan önce üç şey gerekiyor:** (1) `publicfeedsettings` migration'ının uygulanması, (2) ilgili tenant'ta `IsPublicFeedEnabled = true` + `PublicSlug = 'altinkose'`, (3) nginx'te rate limiting (D7). |
| 2026-08-15 | **Adım 7 — Public fiyat yayını, BACKEND tamamlandı.** `Tenant`'a `IsPublicFeedEnabled` (varsayılan `false`) ve `PublicSlug` eklendi; `[AllowAnonymous]` `PublicPriceHub` (`/publichub`) ve `GET /api/public/tenant/{slug}` açıldı; yayıncı bayrağı açık tenant'lar için `PricePlatform.Public` listesi üretiyor. Fiyatlar tenant'ın standart çarpanlarıyla hesaplanıyor, viewer override'ı uygulanmıyor. **Abonelik kontrolü public'te tek fren** olduğu için `GetPublicFeedTenantIdsAsync` aynı `SubscriptionEndDate` filtresini uyguluyor. Slug çözümü önbellekli ve **geçersiz slug'lar da kısa süre önbelleğe alınıyor** (anonim uç noktada DB'yi dövmesin). **Kullanıcı kararı: public ekran yalnızca `cilent/altinkose` müşterisinde açılacak; `client/cilcil-altin` yalnızca performans iyileştirmelerini alacak.** ⚠️ **Migration üretildi (`publicfeedsettings`) ama CANLIYA UYGULANMADI.** ⚠️ Migration el ile sadeleştirildi: EF'in ürettiği hâli 56 KB'tı ve 66 `SystemCatalog` satırını silip yeniden ekliyordu — kök neden yeni bulgu **M18** olarak kaydedildi. Derleme: **0 hata**. |
| 2026-08-15 | **🔴 KRİTİK DÜZELTME — grup değişiminde fiyatların donması.** Yayın mesajlarına `feed` (yayın grubu adı) alanı eklendi ve istemciler farklı akışa ait delta'ları artık yok sayıyor. **Bulunan hata:** `seq` numaraları grup başına ilerliyor; bir viewer'a özel fiyat tanımlandığında istemci standart gruptan özel gruba taşınıyor ve iki grubun numaraları bağımsız (standart grup binlerde, yeni özel grup 1'den başlıyor). Taşınma anında yolda olan eski gruba ait bir delta, yeni grubun snapshot'ından sonra ulaşırsa hem yanlış fiyat uygulanıyor hem de `lastSeq` çok yükseğe çıktığı için sonraki tüm doğru delta'lar yok sayılıyor — **ekran 60 saniyeye kadar donuyordu.** Tam olarak "viewer'a özel fiyat tanımla/sil" senaryosunda, yani ürünün canlı bir özelliğinde tetikleniyordu. Üç repoda birden düzeltildi; mobil düzeltme her iki müşteri branch'ine de cherry-pick edildi. Doğrulama: API **0 hata**, Angular `build:prod` **başarılı**, `flutter analyze` **0 hata**. |
| 2026-08-15 | **Abonelik kontrolü yayına eklendi + üç repo senkronizasyon doğrulaması.** `GetActiveTenantIdsAsync` artık `SubscriptionEndDate`'i de filtreliyor; aboneliği biten tenant'ın yayını duruyor (öncesinde kontrol yalnızca login'deydi, açık ekranlar akmaya devam ediyordu — public yayında gelir kaçağı olurdu). Zincirleme sonuç olarak `SubscriptionService`'in dört yazma noktasına `ClearTenantCache()` eklendi; aksi halde aboneliği yenilenen tenant 10 dakika yayına dönemezdi. Dairesel bağımlılık kontrol edildi. **Senkronizasyon doğrulaması:** event adları, JSON alan adları, `ClientSource`→`PricePlatform` eşlemesi ve `seq` kuralları üç tarafta birebir tutuyor; **iki gerçek fark bulunup giderildi** — mobilde eksik `onreconnecting` (bayat listeye delta uygulama riski) ve delta'daki `null` fiyat davranışı (mobil eski fiyatı koruyordu, web temizliyordu). İki ölü referans temizlendi. Doğrulama: API **0 hata**, Angular `build:prod` **başarılı**, `flutter analyze` **0 hata**. |
| 2026-08-15 | **Adım 6 — İstemci performansı.** **Web:** dashboard `OnPush`'a geçirildi; ürünler `id` ile indeksleniyor (O(1)), kategori listeleri aynı nesnelerin referansını tutuyor (kopya veri yok) ve yalnızca ürün kümesi/sırası değişince yeniden kuruluyor, flash temizleme tek zamanlayıcıya indi, paket başına tek `markForCheck` (öncesinde ürün başına `detectChanges`). W1, W2, W3, W4, W8 kapandı. **Mobil:** kategori gruplaması ekranın `build`'inden `GoldHubProvider`'a taşındı ve yalnızca snapshot'ta hesaplanıyor (M5); ekranın kökündeki `context.watch` kaldırılıp fiyata bağlı iki bölge `Consumer`'a alındı, böylece AppBar/drawer/alt sekmeler fiyat tikinde yeniden çizilmiyor (M4 büyük ölçüde). Doğrulama: Angular `build:prod` **başarılı**, `flutter analyze` **0 hata** (uyarı 106 → 104). Backend'e dokunulmadı. **Bu adımla birlikte Adım 7'nin (public dashboard) tüm teknik ön koşulları tamamlandı.** |
| 2026-08-15 | **Adım 5 — API sözleşmesi + delta yayını. ⚠️ ÜÇ REPODA BİRDEN KIRICI DEĞİŞİKLİK.** **M3 çözüldü:** fiyat hesabı tek bir `PriceCalculator`'a çıkarıldı; mock'un sahte veri üretmesi hesabın içinden alınıp çağrı öncesine taşındı, böylece lokal ile canlı aynı kuralla çalışıyor. **Sözleşme:** `PriceData` (Datshop'tan GELEN) ile istemciye GİDEN DTO ayrıldı; yeni `PriceItemDto`'ya **`id`** ve **`sourceKey`** eklendi, **`description`** (ürün adının kopyasıydı), **`source`** (sabit "Sarfiyum") ve satır başına **`timestamp`** kaldırıldı, fiyatlar **4 haneye yuvarlanıyor** (çarpanlar `decimal(18,10)` olduğu için `5688.7300000000` gibi seri hale geliyordu). Ölçüm: satır başına ~240 byte'ın ~%60'ı fazlalıktı. **P4 çözüldü:** görünürlük filtresi sunucuya taşındı → yayın grupları platform bazlı oldu, gizli ürünler tel üzerinden hiç gitmiyor. **P2 çözüldü:** snapshot + delta; `IPriceFeedStateTracker` grup başına son durumu tutuyor, ürün yapılandırması değişince otomatik snapshot'a düşüyor (ayrı bildirim mekanizması gerekmedi), 60 sn'de bir tam senkron sapmayı sınırlıyor, hiçbir fiyat oynamadıysa hiç mesaj gitmiyor. `GoldHub.OnConnectedAsync` bağlantı anında tam liste gönderiyor. **Web:** `id` bazlı eşleştirme, istemci filtresi kaldırıldı, `description` template'ten çıkarıldı; ayrıca **SignalR dinleyicilerinin `start()` sonrası kaydedilmesi bug'ı düzeltildi** (bağlantı anındaki snapshot kaçıyordu). **Mobil:** aynısı + **M2** (`sourceKey` ile DOLAR/EURO başlığı), **M3** (ürün başına timer yerine tek flash zamanlayıcısı), **M13** (satır başına `timestamp` kalktığı için `DateTime.parse` exception riski ortadan kalktı). Doğrulama: API **0 hata**/64 uyarı, Angular `build:prod` **başarılı** (initial 2.55 MB), `flutter analyze` **0 hata** (uyarı 106 → 104). Migration/şema değişikliği **yok**. |
| 2026-08-15 | **Adım 4 — Yayın performansı.** **P1 çözüldü:** yayın gelen paket frekansından koparıldı. `price_changed` artık yalnızca `_priceCache`'i güncelleyip "kirli" bayrağı bırakıyor; yayını sabit aralıklı `RunBroadcastLoopAsync` yapıyor (varsayılan 1000 ms, `SocketServer:BroadcastIntervalMs`). Aradaki paketler birleştiriliyor, yeni veri yoksa tick boşa dönüyor (piyasa sakinken sıfır yayın). Watchdog ayrı döngüye alındı. `[OVERLAP]` uyarısının anlamı değişti: artık "yayın turu tick aralığından uzun sürüyor" demek — sık görülürse aralık artırılmalı veya P2/P3 yapılmalı. **P11 büyük ölçüde hafifledi** (allocation frekansı düştü; ayrıca viewer özel ayarları için ürün başına `FirstOrDefault` yerine tek seferlik `Dictionary` indeksi kuruldu, liste kapasitesi önden veriliyor). **Kapsam kararları:** **P2 (delta) Adım 5'e taşındı** — ikisi de tel formatını değiştiriyor, ayrı yapmak istemcileri iki kez değiştirmek olurdu. **P3'ün önceliği düşürüldü** — DI scope + cache maliyeti artık saniyede en fazla bir kez oluşuyor. Derleme: **0 hata**, uyarı 64 (değişmedi). API sözleşmesi değişmedi → **web ve mobil etkilenmiyor.** |
| 2026-08-15 | **Adım 2.5 — Sunucu yapılandırması uygulandı** (kullanıcı tarafından, `/opt/sarfiyum`). **D1** log rotation (`x-logging` anchor, 10m × 5, üç servise de), **D2** `client_max_body_size 10M` (logo yükleme 1 MB hatası kapandı), **D3** değişkenli `proxy_pass` + runtime DNS çözümü (deploy'daki 502 penceresi kapandı), **D4** `map $http_upgrade $connection_upgrade`, **D5** eksik 80→443 blokları. Kalan: D6, D7 ve `deploy.yml`'deki artık gereksiz nginx restart'ı (§5.3). |
| 2026-08-15 | **Adım 3 — Oturum & hub hattı.** **G10** çözüldü: `POST /api/auth/logout` eklendi; `UserActiveSession` satırını siler, oturum önbelleğini düşürür, `ForceCheckSession` yollar ve açık SignalR bağlantılarını yayın gruplarından çıkarır (istemci sinyali yok saysa bile fiyat akışı sunucuda kesilir). Token'daki `SessionId` DB'dekiyle karşılaştırıldığı için eski bir token yeni bir oturumu kapatamaz. **P5/P6** çözüldü: yeni `IActiveSessionCache` (Core arayüz + Service implementasyon, 5 dk) ile oturum doğrulaması artık her istekte ve her hub bağlantısında DB'ye gitmiyor; tek yazar `AuthService` olduğu için önbellek noktasal geçersizleştiriliyor ve **tek cihaz kuralının anındalığı korunuyor**. **P8** çözüldü: `BroadcastToTenants` bağlı olmayan özel viewer için artık liste hesaplamıyor (`IHubConnectionRegistry`); atlanan sayı `[PERF]` logunda. **M14** kapatıldı ve **önceki notu düzeltildi** — hub bağlantısı koptuğunda `UserActiveSession`'ı silmek *yanlış* olurdu (sayfa yenilemede oturum düşerdi); satır yalnızca çıkışta silinir. **Üç repo birden:** web `AuthenticationService.logout()` ve mobil `AuthProvider.logout()` yeni endpoint'i çağırıyor (mobil M-G3). Derleme: **0 hata**, uyarı 64 (değişmedi); Angular `tsc --noEmit` temiz; `flutter analyze` yeni sorun üretmedi. Migration/şema değişikliği **yok**. |
| 2026-08-15 | **Sunucu yapılandırması incelendi** (`/opt/sarfiyum/docker-compose.yml` + `nginx.conf`). §5.1 (yapı) ve §5.2 (D1–D7 bulguları) eklendi. En önemlisi **D2**: `client_max_body_size` hiçbir yerde tanımlı olmadığı için nginx varsayılanı 1 MB → **1 MB'tan büyük firma logosu yüklenemiyor, canlıda aktif bir hata.** Ayrıca **D3**: sarfiyum bloklarında `proxy_pass` değişkensiz kullanıldığı için nginx backend IP'sini yalnızca açılışta çözüyor; deploy'daki `docker restart sarfiyum_nginx` satırının sebebi bu ve her deploy'da tüm siteleri kesiyor. Bu bulgular **kod değil sunucu tarafı** işidir; uygulama kullanıcıya bırakıldı. |
| 2026-08-15 | **Adım 2 — Log gürültüsü & watchdog.** P9 çözüldü: yayın hattındaki rutin loglar `Debug`'a indirildi, tekrarlayan uyarılar (`[OVERLAP]`, `[PRICE_CONFIG]`) susturulmadan **throttle** edildi (5 dk). P10 çözüldü: watchdog eşiği 15→60 sn, ardışık sıfırlamalarda üstel backoff (üst sınır 300 sn), veri dönünce tek satırlık toparlanma logu; eşik ve üst sınır `appsettings.json → SocketServer` altında yapılandırılabilir. `[SOCKET_IN]`'in `async` lambda'sındaki CS1998 uyarısı da kapandı. Derleme: **0 hata**, uyarı 65 → 64. Migration/şema değişikliği **yok**, API sözleşmesi değişmedi → **web ve mobil tarafı etkilenmiyor.** ⚠️ Kalan iş **P9b**: sunucudaki `/opt/sarfiyum/docker-compose.yml` içine `json-file` log rotation (`max-size`, `max-file`) elle eklenmeli. Ayrıca §10.5 **yol haritası** dokümana kalıcı olarak eklendi. |
| 2026-08-15 | **Adım 1 — Görünür bug'lar.** M1 (viewer grup senkronizasyonu) `IHubConnectionRegistry` + `IViewerPricingNotifier` ile çözüldü; `GoldHub.OnDisconnectedAsync` eklendi. M2 (30 sn kör nokta) çalışmayan online filtresi kaldırılarak çözüldü, cache 1 saate çıkarıldı, metot/cache anahtarı yeniden adlandırıldı. M4 (sessiz `null` fiyat) çözüldü + `[PRICE_CONFIG]` uyarı logu eklendi. G7, M15, M16 çözüldü. P8 ve M14 kısmen ele alındı. Derleme: **0 hata**, uyarı 63 → 61. Migration/şema değişikliği **yok**. |

---

## 13. TEST PLANI (lokal)

> Bu bölüm, 2026-08-15'te yapılan Adım 1–7 çalışmasının **tamamını** doğrulamak içindir.
> Amaç yalnızca yeni özellikleri değil, **dokunulan her yerin bozulmadığını** da görmektir.

### 13.0 Neden lokalde birebir test edilebilir

Datshop yalnızca canlı sunucunun IP'sini whitelist'e aldığı için lokalde gerçek veri akmaz.
Bunun yerine `MockGoldPriceHostedService` çalışır ve **canlıyı taklit eder**:

- Semboller `SystemCatalog`'dan okunur → canlıda Datshop'un gönderdiği **66 sembolün aynısı**.
- Paketler 400 ms'de bir üretilir (Datshop da saniyede birkaç paket gönderir), yayın ise
  1000 ms'lik tick ile yapılır → **coalescing gerçekten test edilir**.
- Her pakette sembollerin ~%35'i oynar → **delta'nın kısmi çalıştığı görülür**.
- ⚠️ **Kritik:** yayın mantığının tamamı (`IPriceBroadcaster` + `PriceFeedHostedServiceBase`)
  mock ve canlı tarafından **ortak** kullanılır. Yani lokalde test edilen kod, canlıda
  çalışacak kodun ta kendisidir. (Eskiden iki kopyaydı ve ayrışmıştı — bulgu M3.)

### 13.1 Kurulum (bir kez)

```bash
# 1) Lokal DB olustur - ORTAM DEGISKENI SART, yoksa EF canliya baglanir!
cd c:\Users\yvzsy\source\repos\SarfiyumAPI
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update -p SarfiyumAPI.Data -s SarfiyumAPI

# 2) API'yi baslat  -> https://localhost:7140  (Swagger: /swagger)
dotnet run --project SarfiyumAPI

# 3) Web'i baslat   -> http://localhost:4200
cd c:\Users\yvzsy\OneDrive\Desktop\SarfiyumUI
npm start
```

`appsettings.Development.json` connection string'i **lokal SQLEXPRESS**'e (`SarfiyumDB_Local`)
çevirir; canlı veritabanına dokunulmaz.

**Otomatik oluşan test verisi** (`DevDataSeeder`, idempotent):

| Kullanıcı | Rol | Not |
|---|---|---|
| `yvzsylu` | Admin | Mevcut seed (şifresi `AppDbContext`'te) |
| `kuyumcu` | User | Test Kuyumculuk firması |
| `musteri1` | Viewer | Özel fiyatı **yok** — standart grup testi |
| `musteri2` | Viewer | Özel fiyat testleri bunun üzerinde |

Şifre: **`Test1234!`** · Public slug: **`testkuyumcu`** · Abonelik: 1 yıl, public yayın **açık**.

**Mock'u yönlendirme** (`appsettings.Development.json` → `MockFeed`):
`PacketIntervalMs` (paket sıklığı) · `Paused: true` (piyasa kapalı) · `Volatility` (oynaklık).
Ayrıntılı log için `Logging:LogLevel:Default` → `"Debug"`.

---

### 13.2 A — FİYAT AKIŞI VE YENİ SÖZLEŞME

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| A1 | Dashboard açılışı | `kuyumcu` ile gir | Fiyatlar **anında** gelir (bağlantı anındaki snapshot), boş ekran/gecikme yok |
| A2 | Canlı akış | Ekranı izle | Fiyatlar ~1 sn'de bir oynar, yeşil/kırmızı flash çalışır |
| A3 | Delta çalışıyor mu | `Debug` log + tarayıcı Network → WS çerçeveleri | İlk mesaj `ReceivePriceSnapshot`, sonrakiler `ReceivePriceDelta` ve **yalnızca değişen satırları** taşıyor |
| A4 | 60 sn tam senkron | 1 dk bekle, WS çerçevelerini izle | Dakikada bir yeniden `ReceivePriceSnapshot` gelir |
| A5 | Coalescing | `PacketIntervalMs: 100` yap, yeniden başlat | Paket sıklığı 10× artar ama **yayın yine ~1 sn'de bir** olur |
| A6 | Piyasa kapalı | `MockFeed:Paused: true`, yeniden başlat | **Hiç mesaj gitmez**, ekran son fiyatlarla durur, CPU boşta |
| A7 | Payload temizliği | WS çerçevesini incele | `description`, `source`, satır başına `timestamp` **YOK**; `id`, `sourceKey` **VAR**; `bid/ask` en fazla 4 ondalık |
| A8 | Fiyat yoksa | Ürün & Çarpan'da bir ürüne katalogda olmayan `SourceKey` ver | Ürün listede **görünmez** (canlıdaki davranışın aynısı) |

### 13.3 B — ÜRÜN KİMLİĞİ VE GÖRÜNÜRLÜK

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| B1 | **Ürün adı değiştirme** | Ürün & Çarpan'da bir ürünün adını değiştir, kaydet | Dashboard'da satır **ikinci kez eklenmez**, adı güncellenir. (Eski hata: aynı ürün iki satır olurdu) |
| B2 | Aynı isimli iki ürün | İki farklı kategoriye aynı adla ürün ekle | İkisi de ayrı satır olarak doğru görünür, karışmaz |
| B3 | **Web'de gizle** | Bir ürünün web görünürlüğünü kapat | Web'den kaybolur, **mobilde durmaya devam eder** |
| B4 | **Mobilde gizle** | Mobil görünürlüğünü kapat | Mobilden kaybolur, web'de kalır |
| B5 | Gizli ürün tel üzerinde | Gizlerken WS çerçevesini izle | Gizlenen ürün **hiç gönderilmiyor** (istemcide filtrelenmiyor) |
| B6 | Ürün ekleme/silme | Yeni ürün ekle, sonra sil | Dashboard'a otomatik `ReceivePriceSnapshot` gelir, liste güncellenir |
| B7 | Sıralama | Sürükle-bırak sırala, kaydet | Yeni sıra dashboard'a yansır (snapshot ile) |
| B8 | Kategori adı değiştirme | Kategoriler'den bir adı değiştir | Dashboard başlığı güncellenir |

### 13.4 C — VIEWER'A ÖZEL FİYAT ⚠️ EN KRİTİK BÖLÜM

> Delta mimarisinin en kırılgan noktası burasıdır. Grup değişimi sırasında sıra numarası
> yarışı yaşanıyordu ve ekran 60 sn'ye kadar donuyordu (`feed` alanıyla çözüldü).

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| C1 | **Bağlıyken özel fiyat tanımla** | `musteri2` ile ikinci tarayıcıda gir, dashboard açık kalsın. `kuyumcu` ile ona özel fiyat tanımla | Viewer ekranı **anında** özel fiyata geçer. **Titreme yok, donma yok.** Sonraki saniyelerde fiyatlar akmaya devam eder |
| C2 | **Bağlıyken özel fiyatı sil** | Aynı viewer bağlıyken özel ayarı kaldır | Standart fiyata **anında** döner, akış kesilmez |
| C3 | Ard arda değiştir | 3–4 kez tanımla/sil | Her seferinde doğru listeye geçer, hiçbir aşamada donmaz |
| C4 | Özel fiyat doğruluğu | Çarpan/makas gir | Viewer'ın gördüğü fiyat = kaynak × özel çarpan ± makas |
| C5 | Diğer viewer etkilenmiyor | `musteri1` da bağlıyken C1'i yap | `musteri1` standart fiyatta kalır |
| C6 | **Bağlı olmayan viewer** | `musteri2`nin özel fiyatı varken çıkış yap, `Debug` logu izle | `[PERF]` satırında `atlanan çevrimdışı viewer: 1` görülür — listesi hesaplanmıyor |

### 13.5 D — OTURUM, ÇIKIŞ, TEK CİHAZ

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| D1 | **Çıkış → akış kesiliyor mu** | Dashboard açıkken çıkış yap | Fiyat akışı **sunucuda** kesilir; `UserActiveSessions`'ta satır silinir (DB'den doğrula) |
| D2 | Tek cihaz (web) | Aynı kullanıcıyla ikinci tarayıcıda gir | İlk tarayıcı **anında** düşer |
| D3 | Tek cihaz (platform ayrı) | Web'de `kuyumcu`, mobilde `kuyumcu` | **İkisi de açık kalır** — kural platform başına işler |
| D4 | Çıkışta diğer platform | Web'den çıkış yap, mobil açıkken | **Mobil etkilenmez** |
| D5 | **Sayfa yenileme** | Dashboard'da F5 | Oturum **düşmez**, fiyatlar geri gelir. (Hub kopması oturumu silmemeli) |
| D6 | Eski token ile çıkış | İki cihazdan gir, eskisinin token'ıyla `logout` çağır (Swagger) | Yeni oturum **kapanmaz** |
| D7 | Çift çıkış | Aynı çıkışı iki kez çağır | İkisi de başarı döner (idempotent) |
| D8 | Oturum önbelleği | Art arda çok istek at, SQL profilini izle | `UserActiveSessions` sorgusu **her istekte çalışmaz** |
| D9 | Viewer token süresi | `musteri1` ile gir, 5 dk bekle | Otomatik çıkış olur |
| D10 | Şifre sıfırlama | `kuyumcu` için talep et | SMTP ayarlıysa e-posta gider; **Viewer için reddedilir** |

### 13.6 E — ABONELİK

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| E1 | **Abonelik bitince yayın duruyor mu** | DB'de `Tenants.SubscriptionEndDate`'i geçmişe çek, ≤10 dk bekle (veya API'yi yeniden başlat) | Açık ekranlara **fiyat gitmez**. (Eskiden akmaya devam ederdi) |
| E2 | **Abonelik yenilenince dönüyor mu** | Admin panelden abonelik ekle / gün ekle | Yayın **birkaç saniye içinde** geri gelir (önbellek temizleniyor) |
| E3 | Aboneliksiz giriş | Abonelik bitmişken giriş dene | 403 "Abonelik doldu" |
| E4 | Firma pasif | Tenant'ı pasife çek | Giriş 403, yayın durur |

### 13.7 F — PUBLIC (GİRİŞSİZ) EKRAN — backend

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| F1 | Tenant bilgisi | `GET /api/public/tenant/testkuyumcu` | Ad, logo, 5 kategori döner |
| F2 | Geçersiz slug | `GET /api/public/tenant/yokboyle` | **404** |
| F3 | Bayrak kapalı | DB'de `IsPublicFeedEnabled = false` yap | **404** (aynı cevap — bilgi sızmaz) |
| F4 | Abonelik bitmiş | E1'i yapıp public'i dene | **404**, yayın durur |
| F5 | Public fiyatlar standart mı | `musteri2`ye özel fiyat tanımlıyken public'i izle | Public **standart** fiyatları gösterir, özel fiyatı değil |
| F6 | Public görünürlük | Bir ürünün mobil görünürlüğünü kapat | Public listeden de çıkar |
| F7 | Slug'sız bağlantı | `/publichub` slug'sız bağlan | Bağlantı reddedilir |

### 13.8 G — YÖNETİM EKRANLARI (regresyon — bozulmadığını doğrula)

| # | Alan | Kontrol |
|---|---|---|
| G1 | Admin · Firmalar | Listele, ekle, düzenle, detay, aktif/pasif, sil |
| G2 | Admin · Abonelikler | Ekle, gün ekle/çıkar, düzenle, sil |
| G3 | Admin · Kullanıcılar & Roller | CRUD + rol atama |
| G4 | Admin · Dashboard | Sayaçlar ve grafikler doluyor |
| G5 | Admin · SMTP ayarları | Oku/kaydet |
| G6 | Kuyumcu · Ürün & Çarpan | Ekle, düzenle, sil, sırala, toplu kaydet, virgüllü ondalık |
| G7 | Kuyumcu · Kategoriler | Ad güncelleme |
| G8 | Kuyumcu · Genel Ayarlar | WhatsApp numarası |
| G9 | Kuyumcu · Viewer yönetimi | Ekle, düzenle (şifre dahil), aktif/pasif, sil |
| G10 | Kuyumcu · **Logo yükleme** | Küçük ve **1 MB üstü** logo dene (lokalde nginx yok, ikisi de geçmeli) |
| G11 | Kuyumcu · Profil | Bilgi güncelleme |

### 13.9 H — DASHBOARD UI (OnPush sonrası regresyon)

> `OnPush`'a geçiş, "template dışından yapılan değişiklik ekrana yansımıyor" tipi hatalara
> açıktır. Bu bölüm özellikle onu arar.

| # | Senaryo | Beklenen |
|---|---|---|
| H1 | **Canlı saat** | Saniye sayacı akıyor (donmuyor) |
| H2 | **Kategori başlıkları** | Açılışta API'den gelen adlar görünüyor |
| H3 | Tema değiştirme | 7 temanın hepsi anında uygulanıyor |
| H4 | Özel renk düzenleyici | Renk değişimi canlı önizleniyor, kaydet çalışıyor |
| H5 | Font boyutu/kalınlığı | Öğe bazlı ayar anında uygulanıyor, yenilemede korunuyor |
| H6 | Kaydet bildirimi | "Kaydedildi" mesajı çıkıp 3 sn sonra kayboluyor |
| H7 | **TV modu** | Tam ekrana geçiş + çıkışta normal moda dönüş |
| H8 | Kayan bant (ticker) | Üstteki şerit tüm ürünlerle akıyor |
| H9 | Flash efekti | Fiyat değişiminde yeşil/kırmızı yanıp 400 ms sonra sönüyor |
| H10 | Viewer teması | `musteri1` ile gir: tema okunuyor ama **kaydedemiyor** |
| H11 | Uzun süre açık | 15+ dk açık bırak: yavaşlama/donma yok, bellek şişmiyor |

### 13.10 I — BAĞLANTI DAYANIKLILIĞI

| # | Senaryo | Nasıl | Beklenen |
|---|---|---|---|
| I1 | **API yeniden başlatma** | Dashboard açıkken API'yi durdur/başlat | İstemci yeniden bağlanır, **snapshot alır**, fiyatlar geri gelir |
| I2 | Kopukluk sırasında | API kapalıyken ekranı izle | Son fiyatlar durur (ekran boşalmaz) |
| I3 | Ağ kesintisi | DevTools → Network → Offline → tekrar aç | Aynı: yeniden bağlanıp snapshot alır |
| I4 | Uzun kopukluk | 2 dk kapalı tut, sonra aç | Liste **tutarlı** döner, eksik/bayat satır kalmaz |

---

### 13.11 ⚠️ LOKALDE TEST EDİLEMEYENLER — canlıda kontrol edilecek

| # | Konu | Neden lokalde olmaz | Canlıda nasıl doğrulanır |
|---|---|---|---|
| L1 | **Watchdog + üstel backoff** | Watchdog yalnızca gerçek Datshop soketinde çalışır; mock'ta soket yok | Piyasa kapalıyken (gece/hafta sonu) logları izle: `[WATCHDOG]` ilk sıfırlamada bir `Warning`, sonrası `Debug`; reconnect sıklığı dakikalara çıkmalı |
| L2 | **Datshop sembol uyumu** | Mock sembolleri `SystemCatalog`'dan alır; Datshop'un gerçekten aynı anahtarları gönderdiği doğrulanmadı | `Debug` logda `[SOCKET_IN]` paketlerini incele; `SystemCatalog`'da olup gelmeyen anahtar var mı bak (özellikle boşluklu `5 Gr Gram Altın` gibi olanlar) |
| L3 | **nginx `client_max_body_size`** | Lokalde nginx yok | 1 MB üstü logo yükle → 413 almamalı |
| L4 | **nginx runtime DNS (D3)** | Lokalde reverse proxy yok | Deploy sonrası nginx restart etmeden site açılmalı; 502 penceresi olmamalı |
| L5 | **nginx rate limiting (D7)** | Henüz yapılmadı | Public açılmadan önce `limit_req_zone`/`limit_conn_zone` eklenmeli, sonra yük denemesi |
| L6 | **Docker log rotation (D1)** | Lokalde Docker yok | `docker inspect` ile `max-size:10m max-file:5` doğrula; birkaç gün sonra log boyutunu kontrol et |
| L7 | **Gerçek yük / CPU** | Tek istemciyle ölçülmez | Mağaza TV'leri + viewer'lar bağlıyken `docker stats`; `[PERF]` `Warning` satırı **çıkmamalı** |
| L8 | **`[OVERLAP]` uyarısı** | Lokalde yayın turu 1 sn'yi aşmaz | Canlıda görülürse tick aralığı artırılmalı veya P3 yapılmalı |
| L9 | **Migration uygulaması** | Lokal DB'de uygulandı, canlıda **uygulanmadı** | `publicfeedsettings` canlıya uygulanmalı; öncesinde yedek al |
| L10 | **Public tenant ayarı** | Lokalde seeder ayarlıyor | Canlıda Altın Köşe tenant'ı için `IsPublicFeedEnabled = true`, `PublicSlug = 'altinkose'` |
| L11 | **Abonelik önbelleği (10 dk)** | Lokalde yeniden başlatarak kısaltılabilir | Canlıda abonelik bitiminde yayının en fazla 10 dk içinde durduğunu doğrula |
| L12 | **Gerçek fiyat doğruluğu** | Mock rakamları temsilidir | Canlıda birkaç ürünü Datshop değeriyle elle karşılaştır (kaynak × çarpan ± makas) |
| L13 | **E-posta (şifre sıfırlama)** | Lokalde SMTP ayarlı değilse gitmez | Gerçek e-posta ile dene |
| L14 | **Store build'leri** | Emülatör/debug ≠ release | Release imzalı build'lerle son tur test |

---

## 14. CANLIYA ALMA — sıra ve geri dönüş

> 2026-08-15 çalışmasının canlıya alınması. **Sıra kritiktir**; yanlış sırada yapılırsa
> giriş dahil her şey kırılır (aşağıda neden açıklandı).

### 14.1 Commit noktaları (geri dönüş için)

| Repo | Yeni commit | Öncesi (geri dönüş noktası) |
|---|---|---|
| `SarfiyumAPI` | `cbcf6a9` | **`3f46606`** |
| `SarfiyumUI` | `e005b9b` | **`c67d13f`** |
| `sarfiyum_mobile` (`main`) | `684d066` | **`671287c`** |
| `sarfiyum_mobile` (`client/cilcil-altin`) | `3324562` | `b1ec526` |
| `sarfiyum_mobile` (`cilent/altinkose`) | `9720f53` | `ae21beb` |

### 14.2 ⚠️ MIGRATION ÖNCE UYGULANMALI

`Tenant` entity'sine `IsPublicFeedEnabled` ve `PublicSlug` eklendi. EF, `Tenant` okuyan
**her sorguda** bu kolonları da SELECT eder — örneğin `AuthService.LoginAsync` içindeki
`_tenantRepository.GetByIdAsync(...)`.

**Yani migration uygulanmadan yeni kod canlıya çıkarsa `Invalid column name` hatası alınır
ve GİRİŞ DAHİL her şey kırılır.** Kod deploy'undan **önce** uygulanmalıdır.

```bash
# Lokalden canlı DB'ye. Connection string appsettings.json'daki (canlı) olmalı,
# yani ASPNETCORE_ENVIRONMENT set EDİLMEMELİ.
cd c:\Users\yvzsy\source\repos\SarfiyumAPI
dotnet ef database update -p SarfiyumAPI.Data -s SarfiyumAPI
```

Uygulayacağı tek şey (el ile sadeleştirildi, katalog verisine dokunmaz):

```sql
ALTER TABLE [Tenants] ADD [IsPublicFeedEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
ALTER TABLE [Tenants] ADD [PublicSlug] nvarchar(64) NULL;
```

> Öncesinde veritabanı yedeği al. Migration **eklemeli**dir; kod geri alınırsa kolonların
> durması zararsızdır (eski kod onları tanımaz, SELECT'e dahil etmez). Yani **kod rollback'i
> için migration'ı geri almaya gerek yoktur.**

### 14.3 Deploy sırası

| # | Adım | Not |
|---|---|---|
| 1 | **DB yedeği** | Migration öncesi |
| 2 | **Migration uygula** (§14.2) | Kod deploy'undan ÖNCE — zorunlu |
| 3 | **nginx güncelle** (§5.4) | `/publichub` bloğu + rate limiting. `nginx -t` sonra `-s reload` |
| 4 | **`SarfiyumUI` push** (`master`) | Pipeline'ı tetikler; API repo checkout edilip Angular build alınır |
| 5 | **`SarfiyumAPI` push** (`master`) | Asıl deploy. 4 ve 5 aynı imajda birleşir |
| 6 | **Web'i doğrula** | Giriş, dashboard, fiyat akışı, çıkış |
| 7 | **Tenant ayarı** | Altın Köşe için `IsPublicFeedEnabled = 1`, `PublicSlug = 'altinkose'` |
| 8 | **Public'i doğrula** | `curl -i https://sarfiyum.com/api/public/tenant/altinkose` → 200 |
| 9 | **Mobil build** | Üç uygulama; Altın Köşe `cilent/altinkose` branch'inden |

Adım 7 için SQL:

```sql
UPDATE Tenants
SET IsPublicFeedEnabled = 1, PublicSlug = 'altinkose'
WHERE Name LIKE '%Altın Köşe%';   -- doğru satırı önce SELECT ile teyit et
```

> Önbellek 10 dakikalık: ayarı yaptıktan sonra public yayının başlaması birkaç dakika
> alabilir. Hemen görmek için container'ı yeniden başlat.

### 14.4 ⚠️ Mobil uygulamalar geçici olarak çalışmaz

Backend deploy edildiği anda **store'daki mevcut üç mobil sürüm fiyat gösteremez** —
`ReceivePrices` artık gönderilmiyor. Web etkilenmez (API ile aynı imajda gider).

Bu bilinen ve kabul edilen bir durumdur; müşterilere güncelleme çıkana kadar beklemeleri
söylenecektir. Yeni sürümler mümkün olan en kısa sürede store'a gönderilmelidir.

### 14.5 Geri dönüş

**Kod:**
```bash
# API
git -C SarfiyumAPI revert --no-edit cbcf6a9 && git push
# Web
git -C SarfiyumUI revert --no-edit e005b9b && git push
```
`revert` tercih edilir (`reset --hard` + force push değil): geçmiş korunur ve pipeline
normal şekilde tetiklenir.

**Veritabanı:** geri alınmasına gerek yok (§14.2).

**nginx:** `/opt/sarfiyum/nginx.conf.bak-*` dosyasından geri kopyalanır, `nginx -s reload`.

**Mobil:** store sürümü yayınlanmadıysa yapılacak bir şey yok. Yayınlandıysa eski sürüme
dönmek yerine backend'i geri almak daha hızlıdır.

### 14.6 Deploy sonrası ilk 30 dakikada bakılacaklar

- `docker logs -f sarfiyum_backend` → `[PERF]` **Warning** satırı çıkmamalı (yayın turu 1 sn'yi aşmıyor)
- `[OVERLAP]` uyarısı çıkmamalı
- `[PRICE_CONFIG]` uyarısı varsa: o üründe çarpan/makas yanlış girilmiş demektir
- `[WATCHDOG]` ilk sıfırlama dışında sık tekrar etmemeli
- `docker stats` → CPU, önceki duruma göre **düşmüş** olmalı
- Bir mağaza TV'sinde fiyatların aktığını gözle doğrula
