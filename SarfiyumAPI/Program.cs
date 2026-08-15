using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Data.Context;
using SarfiyumAPI.Data.Repositories;
using SarfiyumAPI.Service.Services;
using SarfiyumAPI.Services;
using SarfiyumAPI.Hubs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Coravel;
using SarfiyumAPI.Service.Jobs;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1. TEMEL KONFİGÜRASYONLAR
// ==================================================================

builder.Services.AddControllers();
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ==================================================================
// 2. IDENTITY & AUTHENTICATION
// ==================================================================

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

//Jobs & Scheduler
builder.Services.AddScheduler();
builder.Services.AddTransient<ExpirationJob>();

// ==================================================================
// 3. REAL-TIME & CACHING (Sarfiyum Motoru)
// ==================================================================

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// ==================================================================
// 4. DEPENDENCY INJECTION (Servis Kayıtları)
// ==================================================================

builder.Services.AddScoped<IDataContext>(provider => provider.GetService<AppDbContext>());
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMultiplierSettingsService, MultiplierSettingsService>();
builder.Services.AddScoped<ITenantSettingsService, TenantSettingsService>();
builder.Services.AddScoped<IVisitorSettingService, VisitorSettingService>();


builder.Services.AddScoped<IViewerAccountService, ViewerAccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISessionNotificationService, SarfiyumAPI.Services.SignalRSessionService>();

// Oturum doğrulamasını her istekte DB'ye gitmekten kurtaran önbellek (AGENTS.md P5/P6).
// Scoped: içinde scoped bir repository kullanıyor; önbelleğin kendisi singleton IMemoryCache.
builder.Services.AddScoped<IActiveSessionCache, ActiveSessionCache>();

// Hangi kullanıcının hangi SignalR bağlantılarına sahip olduğunu tutar.
// Singleton olmak ZORUNDA: tüm bağlantılar için tek bir kayıt defteri.
builder.Services.AddSingleton<SarfiyumAPI.Services.IHubConnectionRegistry, SarfiyumAPI.Services.HubConnectionRegistry>();

// Özel fiyat ayarı değişince viewer'ın yayın grubunu canlı olarak güncelleyen servis.
builder.Services.AddScoped<IViewerPricingNotifier, SarfiyumAPI.Services.ViewerPricingNotifier>();

// Fiyat hesabının TEK kaynağı. Singleton: durumsuz (yalnızca uyarı throttle'ı tutuyor),
// hem hosted service hem de ileride başka çağıranlar aynı kuralla çalışmalı.
builder.Services.AddSingleton<IPriceCalculator, PriceCalculator>();

// Yayın grubu başına "en son ne gönderildi" durumu. Snapshot/delta kararını verir.
// Singleton olmak ZORUNDA: durum yayın turları arasında yaşamalı.
builder.Services.AddSingleton<IPriceFeedStateTracker, PriceFeedStateTracker>();

// Bir yayın turunu baştan sona yürüten TEK yer. Hem canlı hem mock veri kaynağı
// buradan geçer; böylece lokalde test edilen davranış canlıdakiyle aynıdır.
builder.Services.AddSingleton<IPriceBroadcaster, SarfiyumAPI.Services.PriceBroadcaster>();

// Önce eski kaydı silin: builder.Services.AddHostedService<GoldPriceHostedService>();

// Yerine bu yapıyı kurun:
if (builder.Environment.IsDevelopment())
{
    // Lokal geliştirme sırasında (localhost) sahte veriler akacak.
    // Datshop yalnızca canlı sunucunun IP'sini whitelist'e aldığı için lokalde gerçek
    // veri akmaz; mock servis onun yerine geçer ve yayın hattının TAMAMI ortak koddan geçer.
    builder.Services.AddHostedService<MockGoldPriceHostedService>();

    // Lokal test ortamını kullanılabilir hâle getirir (firma, kullanıcı, viewer'lar).
    // Idempotenttir, var olanı bozmaz.
    builder.Services.AddHostedService<SarfiyumAPI.Services.DevDataSeeder>();
}
else
{
    // Sunucuda (Production) gerçek Datshop verileri akacak
    builder.Services.AddHostedService<GoldPriceHostedService>();
}


// ==================================================================
// 5. JWT AYARLARI
// ==================================================================

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = false, // Tek uygulama olduğu için false kalabilir
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"], // appsettings.json'dan okuyacak
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/goldhub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});

// ==================================================================
// 6. CORS (GÜNCELLENDİ - Canlı ve Local Ayrımı)
// ==================================================================

builder.Services.AddCors(options =>
{
    // 🔥 PROD: Sadece senin domainin (Güvenlik için şart)
    options.AddPolicy("ProdCors", policy =>
    {
        policy.WithOrigins(
            "https://www.sarfiyum.com", 
            "https://sarfiyum.com", 
            "https://portal.cilcilaltin.com",
            "https://portal.altinkosenadirkuyumculuk.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR için zorunlu
    });

    // 🔥 DEV: Localhost her şeye izin ver
    options.AddPolicy("DevCors", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Scheduler Setup
app.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<ExpirationJob>()
             .DailyAt(0, 0)
             .Zoned(TimeZoneInfo.Local);
});

// ==================================================================
// 7. MIDDLEWARE PIPELINE (SIRALAMA ÇOK ÖNEMLİ)
// ==================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Localde DevCors politikasını kullan
    app.UseCors("DevCors");
}
else
{
    // Canlıda ProdCors politikasını kullan
    app.UseCors("ProdCors");
}

app.UseHttpsRedirection();

// 🔥 ANGULAR DOSYALARINI SUNMAK İÇİN KRİTİK
app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<SarfiyumAPI.Middlewares.SessionCheckMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GoldHub>("/goldhub");

// Girişsiz (public) fiyat ekranı. Ayrı hub: GoldHub'ın oturum/tek-cihaz mantığına
// hiç dokunmadan izole kalır. Erişim, query'deki `slug`'ın public yayını açık bir
// tenant'a çözülmesine bağlıdır (bkz. PublicPriceHub).
// ⚠️ Anonim uç nokta: nginx tarafında bağlantı/istek limiti uygulanmalı (AGENTS.md D7).
app.MapHub<PublicPriceHub>("/publichub");

// 🔥 EN ÖNEMLİ EKLEME: SPA FALLBACK 🔥
// Eğer gelen istek API değilse ve dosya değilse (resim, css vs.),
// kullanıcıyı Angular'ın index.html sayfasına gönder.
// Böylece sayfayı yenilediğinde 404 hatası almazsın.
app.MapFallbackToFile("index.html");

app.Run();