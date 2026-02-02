using SarfiyumAPI.Core.DTOs;
using SarfiyumAPI.Core.Entities;
using SarfiyumAPI.Core.Wrapper;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface IVisitorSettingService
    {
        // 1. HostedService ve Hub İçin Kritik Metotlar
        Task<bool> HasSpecialSettingsAsync(string visitorId);
        Task<List<VisitorSessionDto>> GetOnlineVisitorsWithSettingsAsync(string tenantId);

        // 2. UI/Controller İçin Metotlar
        Task<ServiceResult<string>> UpsertSettingAsync(string tenantId, VisitorProductSettingDto model);
        Task<ServiceResult<string>> DeleteSettingAsync(string tenantId, string visitorId, string productId);

        // Belirli bir ziyaretçiye atanmış tüm özel ayarları listeleme (UI için)
        Task<ServiceResult<List<VisitorProductSetting>>> GetSettingsByVisitorAsync(string tenantId, string visitorId);

        // Cache Temizleme
        void ClearCache(string tenantId);
    }
}