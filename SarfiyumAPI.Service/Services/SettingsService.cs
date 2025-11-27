using Microsoft.EntityFrameworkCore;
using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Interfaces;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Service.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IRepository<AppSetting> _repository;
        private readonly IDataContext _context;

        public SettingsService(IRepository<AppSetting> repository, IDataContext context)
        {
            _repository = repository;
            _context = context;
        }

        // YARDIMCI METOT: Ayar kaydını bul, yoksa oluştur.
        private async Task<AppSetting> GetOrCreateSettingsAsync()
        {
            var settingsList = await _repository.GetAllAsync();
            var settings = settingsList.FirstOrDefault();

            if (settings == null)
            {
                settings = new AppSetting
                {
                    // Varsayılan değerler
                    SmtpHost = "smtp.example.com",
                    SmtpPort = 587,
                    SmtpEmail = "info@example.com",
                    SmtpPassword = "password",
                    SmtpEnableSsl = true,
                    SmtpDisplayName = "Sarfiyum"
                };
                await _repository.AddAsync(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        public async Task<ServiceResult<GlobalSettingsDto>> GetAllSettingsAsync()
        {
            var settings = await GetOrCreateSettingsAsync();

            var dto = new GlobalSettingsDto
            {
                Smtp = new SmtpSettingsDto
                {
                    Host = settings.SmtpHost,
                    Port = settings.SmtpPort,
                    Email = settings.SmtpEmail,
                    Password = settings.SmtpPassword,
                    EnableSsl = settings.SmtpEnableSsl,
                    DisplayName = settings.SmtpDisplayName
                }
                // General = new GeneralSettingsDto { ... } // İleride
            };

            return ServiceResult<GlobalSettingsDto>.Success(dto);
        }

        public async Task<ServiceResult<string>> UpdateSmtpSettingsAsync(SmtpSettingsDto model)
        {
            var settings = await GetOrCreateSettingsAsync();

            settings.SmtpHost = model.Host;
            settings.SmtpPort = model.Port;
            settings.SmtpEmail = model.Email;
            settings.SmtpPassword = model.Password;
            settings.SmtpEnableSsl = model.EnableSsl;
            settings.SmtpDisplayName = model.DisplayName;

            _repository.Update(settings);
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Success("SMTP ayarları güncellendi.", 200);
        }

        public async Task<AppSetting> GetConfigInternalAsync()
        {
            return await GetOrCreateSettingsAsync();
        }
    }
}