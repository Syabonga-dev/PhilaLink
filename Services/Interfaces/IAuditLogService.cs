using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string action, Guid userId);
        Task<List<AuditLog>> GetLogsAsync();
    }
}