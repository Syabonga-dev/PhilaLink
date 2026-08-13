using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class AuditLogService : IAuditLogService
    {
        private readonly PhilaLinkDbContext _context;

        public AuditLogService(PhilaLinkDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, Guid userId)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action,
                PerformedByUserId = userId,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetLogsAsync()
        {
            return await _context.AuditLogs.ToListAsync();
        }
    }
}

