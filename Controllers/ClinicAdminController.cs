using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using PersonalProject.Utilities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinic-admin")]
    [Authorize(Roles = RoleNames.ClinicAdmin)]
    public class ClinicAdminController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        public ClinicAdminController(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context =
                context;

            _audit =
                audit;
        }

        // =====================================================
        // ANALYTICS
        // =====================================================

        [HttpGet("analytics")]
        public async Task<IActionResult>
            GetAnalytics(
                [FromQuery] int months = 6
            )
        {
            try
            {
                var scope =
                    await GetScopeAsync(
                        GetCurrentUserId()
                    );

                var result =
                    await BuildAnalyticsAsync(
                        scope,
                        Math.Clamp(
                            months,
                            3,
                            12
                        )
                    );

                return Ok(
                    result
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // CLINIC STAFF
        // =====================================================

        [HttpGet("staff")]
        public async Task<IActionResult>
            GetStaff(
                [FromQuery] string?
                    role = null
            )
        {
            try
            {
                var scope =
                    await GetScopeAsync(
                        GetCurrentUserId()
                    );

                string? normalizedRole =
                    null;

                if (
                    !string.IsNullOrWhiteSpace(
                        role
                    ) &&
                    !role.Equals(
                        "All",
                        StringComparison
                            .OrdinalIgnoreCase
                    )
                )
                {
                    if (
                        role.Equals(
                            RoleNames.Nurse,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                    )
                    {
                        normalizedRole =
                            RoleNames.Nurse;
                    }
                    else if (
                        role.Equals(
                            RoleNames.Proxy,
                            StringComparison
                                .OrdinalIgnoreCase
                        )
                    )
                    {
                        normalizedRole =
                            RoleNames.Proxy;
                    }
                    else
                    {
                        return BadRequest(
                            new
                            {
                                message =
                                    "Clinic staff role must be Nurse, Proxy, or All."
                            }
                        );
                    }
                }

                var query =
                    _context.Users
                        .AsNoTracking()
                        .Where(
                            user =>
                                (
                                    user.Role ==
                                        RoleNames.Nurse &&
                                    user.Nurse !=
                                        null &&
                                    user.Nurse
                                        .ClinicId ==
                                        scope.ClinicId
                                )
                                ||
                                (
                                    user.Role ==
                                        RoleNames.Proxy &&
                                    user.Proxy !=
                                        null &&
                                    user.Proxy
                                        .ClinicId ==
                                        scope.ClinicId
                                )
                        );

                if (
                    normalizedRole !=
                        null
                )
                {
                    query =
                        query.Where(
                            user =>
                                user.Role ==
                                    normalizedRole
                        );
                }

                var result =
                    await query
                        .OrderBy(
                            user =>
                                user.FullName
                        )
                        .Select(
                            user =>
                                new ClinicAdminStaffDto
                                {
                                    UserId =
                                        user.Id,

                                    FullName =
                                        user.FullName,

                                    IdNumber =
                                        user.IdNumber,

                                    PhoneNumber =
                                        user.PhoneNumber,

                                    Email =
                                        user.Email,

                                    Role =
                                        user.Role,

                                    IsActive =
                                        user.IsActive,

                                    CreatedAt =
                                        user.CreatedAt
                                }
                        )
                        .ToListAsync();

                return Ok(
                    result
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPatch(
            "staff/{userId:guid}/activate"
        )]
        public async Task<IActionResult>
            ActivateStaff(
                Guid userId
            )
        {
            return await SetStaffActive(
                userId,
                true
            );
        }

        [HttpPatch(
            "staff/{userId:guid}/deactivate"
        )]
        public async Task<IActionResult>
            DeactivateStaff(
                Guid userId
            )
        {
            return await SetStaffActive(
                userId,
                false
            );
        }

        // =====================================================
        // REPORT EXPORT
        // =====================================================

        [HttpGet("reports/export")]
        public async Task<IActionResult>
            ExportReport(
                [FromQuery] string
                    format = "pdf",
                [FromQuery] int
                    rangeDays = 30
            )
        {
            try
            {
                if (
                    rangeDays < 1 ||
                    rangeDays > 365
                )
                {
                    return BadRequest(
                        new
                        {
                            message =
                                "Report range must be between 1 and 365 days."
                        }
                    );
                }

                var userId =
                    GetCurrentUserId();

                var scope =
                    await GetScopeAsync(
                        userId
                    );

                var report =
                    await BuildReportAsync(
                        scope,
                        rangeDays
                    );

                var normalized =
                    (
                        format ??
                        string.Empty
                    )
                    .Trim()
                    .ToLowerInvariant();

                byte[] content;

                string contentType;

                string extension;

                switch (
                    normalized
                )
                {
                    case "csv":
                        content =
                            ClinicAdminReportBuilder
                                .BuildCsv(
                                    report
                                );

                        contentType =
                            "text/csv; charset=utf-8";

                        extension =
                            "csv";

                        break;

                    case "excel":
                    case "xlsx":
                        content =
                            ClinicAdminReportBuilder
                                .BuildXlsx(
                                    report
                                );

                        contentType =
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                        extension =
                            "xlsx";

                        break;

                    case "pdf":
                        content =
                            ClinicAdminReportBuilder
                                .BuildPdf(
                                    report
                                );

                        contentType =
                            "application/pdf";

                        extension =
                            "pdf";

                        break;

                    default:
                        return BadRequest(
                            new
                            {
                                message =
                                    "Report format must be csv, xlsx, excel, or pdf."
                            }
                        );
                }

                var clinicFileName =
                    SanitizeFileName(
                        scope.ClinicName
                    );

                var fileName =
                    $"PhilaLink-{clinicFileName}-{DateTime.UtcNow:yyyyMMdd}.{extension}";

                await _audit
                    .LogAsync(
                        "ClinicReportExported",
                        userId,
                        $"{extension.ToUpperInvariant()} clinic report exported for {rangeDays} days.",
                        scope.ClinicId
                    );

                return File(
                    content,
                    contentType,
                    fileName
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // ANALYTICS BUILDER
        // =====================================================

        private async Task<ClinicAdminAnalyticsDto>
            BuildAnalyticsAsync(
                ClinicAdminScope scope,
                int months
            )
        {
            var now =
                DateTime.UtcNow;

            var today =
                now.Date;

            var tomorrow =
                today.AddDays(
                    1
                );

            var monthStart =
                new DateTime(
                    now.Year,
                    now.Month,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc
                );

            var kpis =
                new ClinicAdminKpisDto
                {
                    ActivePatients =
                        await _context.Patients
                            .AsNoTracking()
                            .CountAsync(
                                patient =>
                                    patient.ClinicId ==
                                        scope.ClinicId &&
                                    patient.User.IsActive
                            ),

                    ActiveNurses =
                        await _context.Nurses
                            .AsNoTracking()
                            .CountAsync(
                                nurse =>
                                    nurse.ClinicId ==
                                        scope.ClinicId &&
                                    nurse.User.IsActive
                            ),

                    ActiveProxies =
                        await _context.Proxies
                            .AsNoTracking()
                            .CountAsync(
                                proxy =>
                                    proxy.ClinicId ==
                                        scope.ClinicId &&
                                    proxy.User.IsActive
                            ),

                    AppointmentsToday =
                        await _context.Appointments
                            .AsNoTracking()
                            .CountAsync(
                                appointment =>
                                    appointment.ClinicId ==
                                        scope.ClinicId &&
                                    appointment.ScheduledAt >=
                                        today &&
                                    appointment.ScheduledAt <
                                        tomorrow &&
                                    appointment.Status !=
                                        AppointmentStatuses
                                            .Cancelled
                            ),

                    PendingAppointments =
                        await _context.Appointments
                            .AsNoTracking()
                            .CountAsync(
                                appointment =>
                                    appointment.ClinicId ==
                                        scope.ClinicId &&
                                    appointment.Status ==
                                        AppointmentStatuses
                                            .Pending
                            ),

                    CollectionsDueToday =
                        await _context
                            .MedicationCollections
                            .AsNoTracking()
                            .CountAsync(
                                collection =>
                                    collection.ClinicId ==
                                        scope.ClinicId &&
                                    collection
                                        .ScheduledCollectionDate >=
                                        today &&
                                    collection
                                        .ScheduledCollectionDate <
                                        tomorrow &&
                                    collection.Status !=
                                        MedicationCollectionStatuses
                                            .Collected &&
                                    collection.Status !=
                                        MedicationCollectionStatuses
                                            .Cancelled
                            ),

                    OverdueCollections =
                        await _context
                            .MedicationCollections
                            .AsNoTracking()
                            .CountAsync(
                                collection =>
                                    collection.ClinicId ==
                                        scope.ClinicId &&
                                    collection
                                        .ScheduledCollectionDate <
                                        today &&
                                    collection.Status !=
                                        MedicationCollectionStatuses
                                            .Collected &&
                                    collection.Status !=
                                        MedicationCollectionStatuses
                                            .Cancelled
                            ),

                    LowStockItems =
                        await _context.ClinicStocks
                            .AsNoTracking()
                            .CountAsync(
                                stock =>
                                    stock.ClinicId ==
                                        scope.ClinicId &&
                                    stock.IsActive &&
                                    stock.QuantityOnHand <=
                                        stock.ReorderLevel
                            ),

                    CollectedThisMonth =
                        await _context
                            .MedicationCollections
                            .AsNoTracking()
                            .CountAsync(
                                collection =>
                                    collection.ClinicId ==
                                        scope.ClinicId &&
                                    collection.Status ==
                                        MedicationCollectionStatuses
                                            .Collected &&
                                    collection.CollectedAt !=
                                        null &&
                                    collection.CollectedAt >=
                                        monthStart
                            ),

                    NewPatientsThisMonth =
                        await _context.Patients
                            .AsNoTracking()
                            .CountAsync(
                                patient =>
                                    patient.ClinicId ==
                                        scope.ClinicId &&
                                    patient.CreatedAt >=
                                        monthStart
                            )
                };

            // =================================================
            // MONTHLY ACTIVITY
            // =================================================

            var activityStart =
                monthStart.AddMonths(
                    -(months - 1)
                );

            var patientDates =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.ClinicId ==
                                scope.ClinicId &&
                            patient.CreatedAt >=
                                activityStart
                    )
                    .Select(
                        patient =>
                            patient.CreatedAt
                    )
                    .ToListAsync();

            var appointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            appointment.ClinicId ==
                                scope.ClinicId &&
                            appointment.ScheduledAt >=
                                activityStart
                    )
                    .Select(
                        appointment =>
                            new
                            {
                                appointment
                                    .ScheduledAt,

                                appointment
                                    .Status
                            }
                    )
                    .ToListAsync();

            var collections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .Where(
                        collection =>
                            collection.ClinicId ==
                                scope.ClinicId &&
                            collection
                                .ScheduledCollectionDate >=
                                activityStart
                    )
                    .Select(
                        collection =>
                            new
                            {
                                collection
                                    .ScheduledCollectionDate,

                                collection
                                    .Status
                            }
                    )
                    .ToListAsync();

            var monthlyActivity =
                new List<
                    ClinicAdminTrendPointDto
                >();

            for (
                var i = 0;
                i < months;
                i++
            )
            {
                var start =
                    activityStart
                        .AddMonths(
                            i
                        );

                var end =
                    start.AddMonths(
                        1
                    );

                monthlyActivity.Add(
                    new ClinicAdminTrendPointDto
                    {
                        Period =
                            start.ToString(
                                "MMM yyyy"
                            ),

                        NewPatients =
                            patientDates
                                .Count(
                                    date =>
                                        date >= start &&
                                        date < end
                                ),

                        Appointments =
                            appointments
                                .Count(
                                    item =>
                                        item.ScheduledAt >=
                                            start &&
                                        item.ScheduledAt <
                                            end
                                ),

                        CompletedAppointments =
                            appointments
                                .Count(
                                    item =>
                                        item.ScheduledAt >=
                                            start &&
                                        item.ScheduledAt <
                                            end &&
                                        item.Status ==
                                            AppointmentStatuses
                                                .Completed
                                ),

                        Collections =
                            collections
                                .Count(
                                    item =>
                                        item
                                            .ScheduledCollectionDate >=
                                            start &&
                                        item
                                            .ScheduledCollectionDate <
                                            end
                                ),

                        CompletedCollections =
                            collections
                                .Count(
                                    item =>
                                        item
                                            .ScheduledCollectionDate >=
                                            start &&
                                        item
                                            .ScheduledCollectionDate <
                                            end &&
                                        item.Status ==
                                            MedicationCollectionStatuses
                                                .Collected
                                )
                    }
                );
            }

            // =================================================
            // STATUS BREAKDOWN
            // =================================================

            var statusWindowStart =
                today.AddDays(
                    -30
                );

            var statusWindowEnd =
                tomorrow.AddDays(
                    30
                );

            var appointmentStatuses =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            appointment.ClinicId ==
                                scope.ClinicId &&
                            appointment.ScheduledAt >=
                                statusWindowStart &&
                            appointment.ScheduledAt <
                                statusWindowEnd
                    )
                    .GroupBy(
                        appointment =>
                            appointment.Status
                    )
                    .Select(
                        group =>
                            new ClinicAdminStatusCountDto
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            }
                    )
                    .OrderByDescending(
                        item =>
                            item.Count
                    )
                    .ToListAsync();

            var collectionStatusRows =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .Where(
                        collection =>
                            collection.ClinicId ==
                                scope.ClinicId &&
                            collection
                                .ScheduledCollectionDate >=
                                statusWindowStart &&
                            collection
                                .ScheduledCollectionDate <
                                statusWindowEnd
                    )
                    .Select(
                        collection =>
                            new
                            {
                                collection.Status,

                                collection
                                    .ScheduledCollectionDate
                            }
                    )
                    .ToListAsync();

            var collectionStatuses =
                collectionStatusRows
                    .Select(
                        item =>
                        {
                            if (
                                item.Status ==
                                MedicationCollectionStatuses
                                    .Collected
                            )
                            {
                                return
                                    MedicationCollectionStatuses
                                        .Collected;
                            }

                            if (
                                item.Status ==
                                MedicationCollectionStatuses
                                    .Cancelled
                            )
                            {
                                return
                                    MedicationCollectionStatuses
                                        .Cancelled;
                            }

                            if (
                                item
                                    .ScheduledCollectionDate <
                                today
                            )
                            {
                                return
                                    MedicationCollectionStatuses
                                        .Overdue;
                            }

                            return
                                MedicationCollectionStatuses
                                    .Pending;
                        }
                    )
                    .GroupBy(
                        status =>
                            status
                    )
                    .Select(
                        group =>
                            new ClinicAdminStatusCountDto
                            {
                                Status =
                                    group.Key,

                                Count =
                                    group.Count()
                            }
                    )
                    .OrderByDescending(
                        item =>
                            item.Count
                    )
                    .ToList();

            // =================================================
            // LOW STOCK
            // =================================================

            var lowStock =
                await _context.ClinicStocks
                    .AsNoTracking()
                    .Where(
                        stock =>
                            stock.ClinicId ==
                                scope.ClinicId &&
                            stock.IsActive &&
                            stock.QuantityOnHand <=
                                stock.ReorderLevel
                    )
                    .OrderBy(
                        stock =>
                            stock.QuantityOnHand
                    )
                    .ThenBy(
                        stock =>
                            stock.MedicationName
                    )
                    .Take(
                        8
                    )
                    .Select(
                        stock =>
                            new ClinicAdminLowStockDto
                            {
                                Id =
                                    stock.Id,

                                MedicationName =
                                    stock.MedicationName,

                                Strength =
                                    stock.Strength,

                                Form =
                                    stock.Form,

                                Unit =
                                    stock.Unit,

                                QuantityOnHand =
                                    stock.QuantityOnHand,

                                ReorderLevel =
                                    stock.ReorderLevel
                            }
                    )
                    .ToListAsync();

            // =================================================
            // RECENT AUDIT ACTIVITY
            // =================================================

            var recentActivity =
                await _context.AuditLogs
                    .AsNoTracking()
                    .Where(
                        log =>
                            log.ClinicId ==
                                scope.ClinicId
                    )
                    .OrderByDescending(
                        log =>
                            log.Timestamp
                    )
                    .Take(
                        8
                    )
                    .Select(
                        log =>
                            new ClinicAdminRecentActivityDto
                            {
                                Id =
                                    log.Id,

                                Action =
                                    log.Action,

                                PerformedBy =
                                    log.PerformedByUser ==
                                        null
                                        ? "System"
                                        : log
                                            .PerformedByUser
                                            .FullName,

                                Details =
                                    log.Details,

                                Timestamp =
                                    log.Timestamp
                            }
                    )
                    .ToListAsync();

            return new ClinicAdminAnalyticsDto
            {
                ClinicId =
                    scope.ClinicId,

                ClinicName =
                    scope.ClinicName,

                AdminName =
                    scope.AdminName,

                GeneratedAtUtc =
                    now,

                Kpis =
                    kpis,

                MonthlyActivity =
                    monthlyActivity,

                AppointmentStatuses =
                    appointmentStatuses,

                CollectionStatuses =
                    collectionStatuses,

                LowStockItems =
                    lowStock,

                RecentActivity =
                    recentActivity
            };
        }

        // =====================================================
        // REPORT DATA
        // =====================================================

        private async Task<ClinicAdminReportDataDto>
            BuildReportAsync(
                ClinicAdminScope scope,
                int rangeDays
            )
        {
            var endUtc =
                DateTime.UtcNow;

            var startUtc =
                endUtc.Date.AddDays(
                    -(rangeDays - 1)
                );

            var analytics =
                await BuildAnalyticsAsync(
                    scope,
                    6
                );

            var appointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(
                        appointment =>
                            appointment.ClinicId ==
                                scope.ClinicId &&
                            appointment.ScheduledAt >=
                                startUtc &&
                            appointment.ScheduledAt <=
                                endUtc
                    )
                    .OrderBy(
                        appointment =>
                            appointment.ScheduledAt
                    )
                    .Select(
                        appointment =>
                            new ClinicAdminReportAppointmentDto
                            {
                                ScheduledAt =
                                    appointment
                                        .ScheduledAt,

                                PatientName =
                                    appointment
                                        .Patient
                                        .User
                                        .FullName,

                                Type =
                                    appointment.Type,

                                Mode =
                                    appointment.Mode,

                                Status =
                                    appointment.Status,

                                NurseName =
                                    appointment.Nurse ==
                                        null
                                        ? ""
                                        : appointment
                                            .Nurse
                                            .User
                                            .FullName
                            }
                    )
                    .ToListAsync();

            var collections =
                await _context
                    .MedicationCollections
                    .AsNoTracking()
                    .Where(
                        collection =>
                            collection.ClinicId ==
                                scope.ClinicId &&
                            collection
                                .ScheduledCollectionDate >=
                                startUtc &&
                            collection
                                .ScheduledCollectionDate <=
                                endUtc
                    )
                    .OrderBy(
                        collection =>
                            collection
                                .ScheduledCollectionDate
                    )
                    .Select(
                        collection =>
                            new ClinicAdminReportCollectionDto
                            {
                                ScheduledCollectionDate =
                                    collection
                                        .ScheduledCollectionDate,

                                CollectedAt =
                                    collection.CollectedAt,

                                PatientName =
                                    collection
                                        .Patient
                                        .User
                                        .FullName,

                                Status =
                                    collection.Status,

                                ProxyName =
                                    collection.Proxy ==
                                        null
                                        ? ""
                                        : collection
                                            .Proxy
                                            .User
                                            .FullName,

                                ProcessedByNurseName =
                                    collection
                                        .ProcessedByNurse ==
                                        null
                                        ? ""
                                        : collection
                                            .ProcessedByNurse
                                            .User
                                            .FullName,

                                TotalQuantity =
                                    collection.Items
                                        .Sum(
                                            item =>
                                                (int?)item
                                                    .Quantity
                                        ) ??
                                    0
                            }
                    )
                    .ToListAsync();

            var inventory =
                await _context.ClinicStocks
                    .AsNoTracking()
                    .Where(
                        stock =>
                            stock.ClinicId ==
                                scope.ClinicId
                    )
                    .OrderBy(
                        stock =>
                            stock.MedicationName
                    )
                    .Select(
                        stock =>
                            new ClinicAdminReportStockDto
                            {
                                MedicationName =
                                    stock.MedicationName,

                                Strength =
                                    stock.Strength,

                                Form =
                                    stock.Form,

                                Unit =
                                    stock.Unit,

                                QuantityOnHand =
                                    stock.QuantityOnHand,

                                ReorderLevel =
                                    stock.ReorderLevel,

                                IsActive =
                                    stock.IsActive,

                                Status =
                                    stock.IsActive &&
                                    stock.QuantityOnHand <=
                                        stock.ReorderLevel
                                        ? "Low stock"
                                        : stock.IsActive
                                            ? "Healthy"
                                            : "Inactive"
                            }
                    )
                    .ToListAsync();

            var staff =
                await _context.Users
                    .AsNoTracking()
                    .Where(
                        user =>
                            (
                                user.Role ==
                                    RoleNames.Nurse &&
                                user.Nurse !=
                                    null &&
                                user.Nurse
                                    .ClinicId ==
                                    scope.ClinicId
                            )
                            ||
                            (
                                user.Role ==
                                    RoleNames.Proxy &&
                                user.Proxy !=
                                    null &&
                                user.Proxy
                                    .ClinicId ==
                                    scope.ClinicId
                            )
                    )
                    .OrderBy(
                        user =>
                            user.FullName
                    )
                    .Select(
                        user =>
                            new ClinicAdminStaffDto
                            {
                                UserId =
                                    user.Id,

                                FullName =
                                    user.FullName,

                                IdNumber =
                                    user.IdNumber,

                                PhoneNumber =
                                    user.PhoneNumber,

                                Email =
                                    user.Email,

                                Role =
                                    user.Role,

                                IsActive =
                                    user.IsActive,

                                CreatedAt =
                                    user.CreatedAt
                            }
                    )
                    .ToListAsync();

            var newPatients =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.ClinicId ==
                                scope.ClinicId &&
                            patient.CreatedAt >=
                                startUtc &&
                            patient.CreatedAt <=
                                endUtc
                    )
                    .OrderByDescending(
                        patient =>
                            patient.CreatedAt
                    )
                    .Select(
                        patient =>
                            new ClinicAdminReportPatientDto
                            {
                                FullName =
                                    patient.User
                                        .FullName,

                                PatientNumber =
                                    patient.PatientNumber,

                                PhoneNumber =
                                    patient.User
                                        .PhoneNumber,

                                CreatedAt =
                                    patient.CreatedAt
                            }
                    )
                    .ToListAsync();

            return new ClinicAdminReportDataDto
            {
                ClinicName =
                    scope.ClinicName,

                AdminName =
                    scope.AdminName,

                GeneratedAtUtc =
                    endUtc,

                RangeStartUtc =
                    startUtc,

                RangeEndUtc =
                    endUtc,

                Analytics =
                    analytics,

                Appointments =
                    appointments,

                Collections =
                    collections,

                Inventory =
                    inventory,

                Staff =
                    staff,

                NewPatients =
                    newPatients
            };
        }

        // =====================================================
        // STAFF STATUS
        // =====================================================

        private async Task<IActionResult>
            SetStaffActive(
                Guid userId,
                bool isActive
            )
        {
            try
            {
                var currentUserId =
                    GetCurrentUserId();

                var scope =
                    await GetScopeAsync(
                        currentUserId
                    );

                var user =
                    await _context.Users
                        .Include(
                            item =>
                                item.Nurse
                        )
                        .Include(
                            item =>
                                item.Proxy
                        )
                        .FirstOrDefaultAsync(
                            item =>
                                item.Id ==
                                    userId
                        );

                if (
                    user == null
                )
                {
                    return NotFound(
                        new
                        {
                            message =
                                "Staff account not found."
                        }
                    );
                }

                var belongsToClinic =
                    (
                        user.Role ==
                            RoleNames.Nurse &&
                        user.Nurse !=
                            null &&
                        user.Nurse
                            .ClinicId ==
                            scope.ClinicId
                    )
                    ||
                    (
                        user.Role ==
                            RoleNames.Proxy &&
                        user.Proxy !=
                            null &&
                        user.Proxy
                            .ClinicId ==
                            scope.ClinicId
                    );

                if (
                    !belongsToClinic
                )
                {
                    return Forbid();
                }

                user.IsActive =
                    isActive;

                user.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();

                await _audit
                    .LogAsync(
                        isActive
                            ? "ClinicStaffActivated"
                            : "ClinicStaffDeactivated",
                        currentUserId,
                        $"{user.Role} {user.FullName} ({user.Id}) {(isActive ? "activated" : "deactivated")}.",
                        scope.ClinicId
                    );

                return Ok(
                    new
                    {
                        message =
                            isActive
                                ? "Staff account activated."
                                : "Staff account deactivated."
                    }
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // ADMIN SCOPE
        // =====================================================

        private async Task<ClinicAdminScope>
            GetScopeAsync(
                Guid userId
            )
        {
            var scope =
                await _context.Admins
                    .AsNoTracking()
                    .Where(
                        admin =>
                            admin.UserId ==
                                userId &&
                            admin.User.Role ==
                                RoleNames
                                    .ClinicAdmin &&
                            admin.User.IsActive &&
                            admin.ClinicId !=
                                null
                    )
                    .Select(
                        admin =>
                            new ClinicAdminScope
                            {
                                ClinicId =
                                    admin
                                        .ClinicId!
                                        .Value,

                                ClinicName =
                                    admin.Clinic ==
                                        null
                                        ? ""
                                        : admin.Clinic
                                            .Name,

                                AdminName =
                                    admin.User
                                        .FullName
                            }
                    )
                    .FirstOrDefaultAsync();

            if (
                scope == null ||
                scope.ClinicId ==
                    Guid.Empty
            )
            {
                throw new
                    UnauthorizedAccessException(
                        "Active ClinicAdmin account with an assigned clinic is required."
                    );
            }

            if (
                string.IsNullOrWhiteSpace(
                    scope.ClinicName
                )
            )
            {
                throw new
                    UnauthorizedAccessException(
                        "Assigned clinic could not be resolved."
                    );
            }

            return scope;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string
            SanitizeFileName(
                string value
            )
        {
            var invalid =
                Path
                    .GetInvalidFileNameChars();

            var clean =
                new string(
                    value
                        .Select(
                            character =>
                                invalid.Contains(
                                    character
                                )
                                    ? '-'
                                    : character
                        )
                        .ToArray()
                );

            return clean
                .Trim()
                .Replace(
                    " ",
                    "-"
                );
        }

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
            )
            {
                throw new
                    UnauthorizedAccessException(
                        "Invalid authentication token."
                    );
            }

            return userId;
        }

        private sealed class
            ClinicAdminScope
        {
            public Guid ClinicId
            {
                get;
                init;
            }

            public string ClinicName
            {
                get;
                init;
            } = string.Empty;

            public string AdminName
            {
                get;
                init;
            } = string.Empty;
        }
    }
}