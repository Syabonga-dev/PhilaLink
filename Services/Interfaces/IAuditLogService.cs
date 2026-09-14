using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string action,
            Guid userId,
            string? details = null,
            Guid? clinicId = null
        );

        Task<List<AuditLogResponseDto>>
            GetVisibleLogsAsync(
                Guid requestingUserId
            );
    }
}