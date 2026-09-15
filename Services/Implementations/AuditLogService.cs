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
        private readonly PhilaLinkDbContext _context;

        public AuditLogService(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // WRITE AUDIT LOG
        // =====================================================

        public async Task LogAsync(
            string action,
            Guid userId,
            string? details = null,
            Guid? clinicId = null
        )
        {
            var user =
                await _context.Users
                    .Include(u => u.Admin)
                    .Include(u => u.Nurse)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            userId
                    );

            if (user == null)
            {
                throw new KeyNotFoundException(
                    "User performing audit action was not found."
                );
            }

            /*
             * If the caller does not explicitly provide a clinic,
             * infer clinic scope from the authenticated staff
             * profile.
             */
            if (clinicId == null)
            {
                if (
                    user.Role ==
                        RoleNames.ClinicAdmin &&
                    user.Admin?.ClinicId !=
                        null
                )
                {
                    clinicId =
                        user.Admin.ClinicId;
                }
                else if (
                    user.Role ==
                        RoleNames.Nurse &&
                    user.Nurse != null
                )
                {
                    clinicId =
                        user.Nurse.ClinicId;
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

            _context.AuditLogs.Add(
                log
            );

            await _context
                .SaveChangesAsync();
        }

        // =====================================================
        // READ AUDIT LOG
        // =====================================================

        public async Task<List<AuditLogResponseDto>>
            GetVisibleLogsAsync(
                Guid requestingUserId
            )
        {
            var user =
                await _context.Users
                    .Include(u => u.Admin)
                    .FirstOrDefaultAsync(
                        u =>
                            u.Id ==
                            requestingUserId
                    );

            if (
                user == null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException();
            }

            IQueryable<AuditLog> query =
                _context.AuditLogs
                    .Include(
                        a =>
                            a.PerformedByUser
                    )
                    .Include(
                        a =>
                            a.Clinic
                    );

            if (
                user.Role ==
                RoleNames.SuperAdmin
            )
            {
                /*
                 * SuperAdmin has system-wide audit visibility.
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
                    user.Admin.ClinicId.Value;

                query =
                    query.Where(
                        a =>
                            a.ClinicId ==
                            clinicId
                    );
            }
            else
            {
                /*
                 * Nurses, Patients, Proxies and unsupported
                 * roles must never read the audit history.
                 */
                throw new UnauthorizedAccessException();
            }

            return await query
                .OrderByDescending(
                    a =>
                        a.Timestamp
                )
                .Select(
                    a =>
                        new AuditLogResponseDto
                        {
                            Id =
                                a.Id,

                            Action =
                                a.Action,

                            PerformedBy =
                                a.PerformedByUser ==
                                    null
                                    ? "System"
                                    : a.PerformedByUser
                                        .FullName,

                            Details =
                                a.Details,

                            Timestamp =
                                a.Timestamp
                        }
                )
                .ToListAsync();
        }
    }
}