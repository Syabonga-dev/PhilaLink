using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class AuditLogService :
        IAuditLogService
    {
        private readonly PhilaLinkDbContext
            _context;

        public AuditLogService(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        public async Task LogAsync(
            string action,
            Guid userId,
            string? details = null,
            Guid? clinicId = null
        )
        {
            var user =
                await _context.Users
                    .Include(
                        item =>
                            item.Admin
                    )
                    .Include(
                        item =>
                            item.Nurse
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            userId
                    );

            if (
                user ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "User performing audit action was not found."
                );
            }

            /*
             * When a clinic is not explicitly supplied,
             * infer it from a clinic-scoped staff account.
             *
             * SuperAdmin activity remains system-wide unless
             * the caller explicitly associates the action
             * with a clinic.
             */
            if (
                clinicId ==
                null
            )
            {
                if (
                    user.Role ==
                        RoleNames.ClinicAdmin &&
                    user.Admin?.ClinicId !=
                        null
                )
                {
                    clinicId =
                        user.Admin
                            .ClinicId;
                }
                else if (
                    user.Role ==
                        RoleNames.Nurse &&
                    user.Nurse !=
                        null
                )
                {
                    clinicId =
                        user.Nurse
                            .ClinicId;
                }
            }

            var log =
                new AuditLog
                {
                    Id =
                        Guid.NewGuid(),

                    Action =
                        action,

                    PerformedByUserId =
                        userId,

                    ClinicId =
                        clinicId,

                    Details =
                        details ??
                        string.Empty,

                    Timestamp =
                        DateTime.UtcNow
                };

            _context.AuditLogs
                .Add(
                    log
                );

            await _context
                .SaveChangesAsync();
        }

        public async Task<List<AuditLogResponseDto>>
            GetVisibleLogsAsync(
                Guid requestingUserId
            )
        {
            var user =
                await _context.Users
                    .Include(
                        item =>
                            item.Admin
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            requestingUserId
                    );

            if (
                user ==
                    null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException();
            }

            IQueryable<AuditLog>
                query =
                    _context.AuditLogs
                        .AsNoTracking()
                        .Include(
                            item =>
                                item.PerformedByUser
                        )
                        .Include(
                            item =>
                                item.Clinic
                        );

            if (
                user.Role ==
                RoleNames.SuperAdmin
            )
            {
                /*
                 * SuperAdmin intentionally sees all audit
                 * activity across the platform.
                 */
            }
            else if (
                user.Role ==
                    RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId !=
                    null
            )
            {
                var clinicId =
                    user.Admin
                        .ClinicId
                        .Value;

                query =
                    query.Where(
                        item =>
                            item.ClinicId ==
                            clinicId
                    );
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            return await query
                .OrderByDescending(
                    item =>
                        item.Timestamp
                )
                .Select(
                    item =>
                        new AuditLogResponseDto
                        {
                            Id =
                                item.Id,

                            Action =
                                item.Action,

                            PerformedByUserId =
                                item.PerformedByUserId,

                            PerformedBy =
                                item.PerformedByUser ==
                                null
                                    ? "System"
                                    : item
                                        .PerformedByUser
                                        .FullName,

                            PerformedByRole =
                                item.PerformedByUser ==
                                null
                                    ? "System"
                                    : item
                                        .PerformedByUser
                                        .Role,

                            ClinicId =
                                item.ClinicId,

                            ClinicName =
                                item.Clinic ==
                                null
                                    ? null
                                    : item
                                        .Clinic
                                        .Name,

                            Details =
                                item.Details,

                            Timestamp =
                                item.Timestamp
                        }
                )
                .ToListAsync();
        }
    }
}
