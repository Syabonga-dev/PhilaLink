using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(Guid userId, string message);
        Task<List<Notification>> GetUserNotificationsAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
    }
}