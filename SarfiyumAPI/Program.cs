using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Data.Context;
using SarfiyumAPI.Data.Repositories;
using SarfiyumAPI.Service.Services;
using SarfiyumAPI.Services; // HostedService buradaydı
using SarfiyumAPI.Hubs;     // 🔥 EKLENDİ: GoldHub'ı tanıması için
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Coravel; // Eklendi
using SarfiyumAPI.Service.Jobs; // Job'ın olduğu namespace


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

builder.Services.AddSignalR(); // SignalR servisi
builder.Services.AddMemoryCache(); // Tenant ayarları için RAM önbellek

// ==================================================================
// 4. DEPENDENCY INJECTION (Servis Kayıtları)
// ==================================================================

// Database Context & Repository
builder.Services.AddScoped<IDataContext>(provider => provider.GetService<AppDbContext>());
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Logic Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// 🔥 EKLENEN KRİTİK SERVİSLER 🔥
builder.Services.AddScoped<ITenantSettingsService, TenantSettingsService>(); // Çarpan Hesaplama Logic'i

// Background Worker (Motor)
builder.Services.AddHostedService<GoldPriceHostedService>();

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
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        // SignalR Token'ı QueryString'den okuması için (Angular Bağlantısı İçin Şart)
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
// 6. CORS (ANGULAR BAĞLANTISI)
// ==================================================================

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAngularApp", policy =>
//    {
//        policy.WithOrigins("http://localhost:4200") // Angular Adresin
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials(); // 🔥 SİNYALR İÇİN BU ZORUNLUDUR! Yoksa hata alırsın.
//    });
//});

builder.Services.AddCors(options =>                    // 🔥 TEST SÜRESİNCE HERKESE İZİN VER
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // 🔥 HERKESE İZİN VER (Test bitene kadar)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();


app.Services.UseScheduler(scheduler =>
{
    // "Bu görevi Her Gün gece 00:00'da çalıştır"
    scheduler.Schedule<ExpirationJob>()
             .DailyAt(0, 0)
             .Zoned(TimeZoneInfo.Local); // Sunucunun yerel saatini kullan
});

// ==================================================================
// 7. MIDDLEWARE PIPELINE
// ==================================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAngularApp"); // En üstlerde olması iyidir

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔥 SIGNALR KAPIYI AÇIYORUZ 🔥
app.MapHub<GoldHub>("/goldhub");

app.Run();