using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, Guid userId);
        Task<List<AuditLog>> GetLogsAsync();
    }
}

