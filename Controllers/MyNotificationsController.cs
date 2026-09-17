using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/notifications/me")]
    [Authorize]
    public class MyNotificationsController : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public MyNotificationsController(
            PhilaLinkDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var userId = GetCurrentUserId();

            await EnsureProxyCollectionRemindersAsync(userId);

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(notification =>
                    notification.UserId == userId)
                .OrderByDescending(notification =>
                    notification.CreatedAt)
                .Take(50)
                .Select(notification =>
                    new NotificationResponseDto
                    {
                        Id = notification.Id,
                        UserId = notification.UserId,
                        Message = notification.Message,
                        IsRead = notification.IsRead,
                        CreatedAt = notification.CreatedAt
                    })
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id)
        {
            var userId = GetCurrentUserId();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(item =>
                    item.Id == id &&
                    item.UserId == userId);

            if (notification == null)
            {
                return NotFound(new
                {
                    message = "Notification not found."
                });
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetCurrentUserId();

            var unread = await _context.Notifications
                .Where(notification =>
                    notification.UserId == userId &&
                    !notification.IsRead)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            if (unread.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                updated = unread.Count
            });
        }

        private async Task EnsureProxyCollectionRemindersAsync(
            Guid userId)
        {
            var proxy = await _context.Proxies
                .AsNoTracking()
                .Include(proxyEntity => proxyEntity.User)
                .FirstOrDefaultAsync(proxyEntity =>
                    proxyEntity.UserId == userId &&
                    proxyEntity.User.Role == RoleNames.Proxy &&
                    proxyEntity.User.IsActive);

            if (proxy == null)
            {
                return;
            }

            var patientIds = await _context.ProxyLinks
                .AsNoTracking()
                .Where(link =>
                    link.ProxyId == proxy.Id &&
                    link.IsActive)
                .Select(link => link.PatientId)
                .Distinct()
                .ToListAsync();

            if (patientIds.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var reminderCutoff = now.AddHours(48);

            var collections = await _context.MedicationCollections
                .AsNoTracking()
                .Include(collection => collection.Patient)
                    .ThenInclude(patient => patient.User)
                .Where(collection =>
                    patientIds.Contains(collection.PatientId) &&
                    collection.Status != MedicationCollectionStatuses.Collected &&
                    collection.Status != MedicationCollectionStatuses.Cancelled &&
                    collection.ScheduledCollectionDate <= reminderCutoff)
                .OrderBy(collection =>
                    collection.ScheduledCollectionDate)
                .ToListAsync();

            if (collections.Count == 0)
            {
                return;
            }

            var existingMessages = await _context.Notifications
                .AsNoTracking()
                .Where(notification =>
                    notification.UserId == userId)
                .Select(notification => notification.Message)
                .ToListAsync();

            var existing = existingMessages.ToHashSet();

            foreach (var collection in collections)
            {
                var date = collection.ScheduledCollectionDate
                    .ToString("dd MMM yyyy");

                var message =
                    collection.ScheduledCollectionDate < now
                        ? $"{collection.Patient.User.FullName}'s medication collection is overdue. It was due on {date}."
                        : $"{collection.Patient.User.FullName}'s medication collection is due on {date}.";

                if (existing.Contains(message))
                {
                    continue;
                }

                _context.Notifications.Add(
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Message = message,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                existing.Add(message);
            }

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value) ||
                !Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}
