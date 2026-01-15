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

builder.Services.AddScoped<IViewerAccountService, ViewerAccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISessionNotificationService, SarfiyumAPI.Services.SignalRSessionService>();

// Önce eski kaydı silin: builder.Services.AddHostedService<GoldPriceHostedService>();

// Yerine bu yapıyı kurun:
if (builder.Environment.IsDevelopment())
{
    // Lokal geliştirme sırasında (localhost) sahte veriler akacak
    builder.Services.AddHostedService<MockGoldPriceHostedService>();
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
        policy.WithOrigins("https://www.sarfiyum.com", "https://sarfiyum.com")
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

// 🔥 EN ÖNEMLİ EKLEME: SPA FALLBACK 🔥
// Eğer gelen istek API değilse ve dosya değilse (resim, css vs.),
// kullanıcıyı Angular'ın index.html sayfasına gönder.
// Böylece sayfayı yenilediğinde 404 hatası almazsın.
app.MapFallbackToFile("index.html");

app.Run();