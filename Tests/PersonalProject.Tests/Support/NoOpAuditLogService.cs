using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Tests.Support
{
    internal sealed class
        NoOpAuditLogService :
        IAuditLogService
    {
        public static readonly
            NoOpAuditLogService
            Instance =
                new();

        private NoOpAuditLogService()
        {
        }

        public Task LogAsync(
            string action,
            Guid userId,
            string? details = null,
            Guid? clinicId = null
        )
        {
            return Task.CompletedTask;
        }

        public Task<List<AuditLogResponseDto>>
            GetVisibleLogsAsync(
                Guid requestingUserId
            )
        {
            return Task.FromResult(
                new List<
                    AuditLogResponseDto
                >()
            );
        }
    }
}
