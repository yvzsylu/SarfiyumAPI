using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Interfaces
{
    public interface ISessionNotificationService
    {
        Task SendForceCheckSession(string userId, string sessionId, string clientSource);
    }
}