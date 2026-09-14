using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, Guid userId, string? details = null);

        Task<List<AuditLog>> GetLogsAsync();
    }
}