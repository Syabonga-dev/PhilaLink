using Microsoft.EntityFrameworkCore;
using PhilaLink_Backend.Data;
using PhilaLink_Backend.Models.Entities;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly PhilaLinkDbContext _context;

        public NotificationService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Guid userId, string message)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var n = await _context.Notifications.FindAsync(notificationId);
            if (n == null) return;

            n.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}