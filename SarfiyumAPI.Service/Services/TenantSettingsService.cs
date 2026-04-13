using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class TenantSettingsService : ITenantSettingsService
    {
        private readonly IRepository<TenantSetting> _settingsRepository;
        private readonly IDataContext _context;

        public TenantSettingsService(
            IRepository<TenantSetting> settingsRepository,
            IDataContext context)
        {
            _settingsRepository = settingsRepository;
            _context = context;
        }

        // AYARLARI GETİR (Yoksa varsayılan oluşturur)
        public async Task<ServiceResult<TenantSettingsDto>> GetSettingsAsync(string tenantId)
        {
            var settings = await _settingsRepository
                .Where(x => x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            // Eğer kayıt yoksa, varsayılan boş bir kayıt oluşturup DB'ye atalım
            if (settings == null)
            {
                settings = new TenantSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    WhatsAppNumber = null,
                    CreatedAt = DateTime.UtcNow
                };

                await _settingsRepository.AddAsync(settings);
                await _context.SaveChangesAsync();
            }

            // MANUEL MAPPING (Entity -> DTO)
            var dto = new TenantSettingsDto
            {
                WhatsAppNumber = settings.WhatsAppNumber
            };

            return ServiceResult<TenantSettingsDto>.Success(dto);
        }

        // AYARLARI GÜNCELLE
        public async Task<ServiceResult<string>> UpdateSettingsAsync(string tenantId, UpdateTenantSettingsDto dto)
        {
            var settings = await _settingsRepository
                .Where(x => x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                // Güvenlik: Normalde GetSettings oluşur ama buraya da ekliyoruz
                settings = new TenantSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    ModifiedAt = DateTime.UtcNow
                };
                await _settingsRepository.AddAsync(settings);
            }

            // MANUEL MAPPING (DTO -> Entity)
            settings.WhatsAppNumber = dto.WhatsAppNumber;
            settings.ModifiedAt = DateTime.UtcNow;

            _settingsRepository.Update(settings);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Ayarlar başarıyla güncellendi.", 200);
        }

        public async Task<ServiceResult<ThemeSettingsDto>> GetThemeSettingsAsync(string tenantId)
        {
            var settings = await _settingsRepository
                .Where(x => x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new TenantSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    DashboardTheme = "flat-light",
                    CreatedAt = DateTime.UtcNow
                };
                await _settingsRepository.AddAsync(settings);
                await _context.SaveChangesAsync();
            }

            var dto = new ThemeSettingsDto
            {
                DashboardTheme = settings.DashboardTheme,
                UiSettingsJson = settings.UiSettingsJson,
                CustomColorsJson = settings.CustomColorsJson
            };

            return ServiceResult<ThemeSettingsDto>.Success(dto);
        }

        public async Task<ServiceResult<string>> UpdateThemeSettingsAsync(string tenantId, UpdateThemeSettingsDto dto)
        {
            var settings = await _settingsRepository
                .Where(x => x.TenantId == tenantId)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new TenantSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow
                };
                await _settingsRepository.AddAsync(settings);
            }

            // Sadece tema ayarları güncellenir
            settings.DashboardTheme = dto.DashboardTheme;
            settings.UiSettingsJson = dto.UiSettingsJson;
            settings.CustomColorsJson = dto.CustomColorsJson;
            settings.ModifiedAt = DateTime.UtcNow;

            _settingsRepository.Update(settings);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("Tema ayarları güncellendi.", 200);
        }
    }
}