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
    }
}