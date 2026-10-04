using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using PersonalProject.Utilities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/super-admin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        private readonly IConfiguration
            _configuration;

        private readonly ILogger<SuperAdminController>
            _logger;

        public SuperAdminController(
            PhilaLinkDbContext context,
            IAuditLogService audit,
            IConfiguration configuration,
            ILogger<SuperAdminController> logger
        )
        {
            _context =
                context;

            _audit =
                audit;

            _configuration =
                configuration;

            _logger =
                logger;
        }

        [HttpGet("me")]
        public async Task<IActionResult>
            Me()
        {
            var userId =
                GetCurrentUserId();

            var result =
                await _context.Admins
                    .AsNoTracking()
                    .Where(
                        admin =>
                            admin.UserId ==
                                userId &&
                            admin.User.Role ==
                                RoleNames.SuperAdmin &&
                            admin.User.IsActive
                    )
                    .Select(
                        admin =>
                            new SuperAdminMeDto
                            {
                                UserId =
                                    admin.UserId,

                                AdminId =
                                    admin.Id,

                                FullName =
                                    admin.User
                                        .FullName,

                                Email =
                                    admin.User
                                        .Email,

                                IsActive =
                                    admin.User
                                        .IsActive
                            }
                    )
                    .FirstOrDefaultAsync();

            return result ==
                null
                ? Forbid()
                : Ok(
                    result
                );
        }

        [HttpGet("clinics")]
        public async Task<IActionResult>
            GetClinics()
        {
            var clinics =
                await _context.Clinics
                    .AsNoTracking()
                    .OrderBy(
                        clinic =>
                            clinic.Name
                    )
                    .Select(
                        clinic =>
                            new ClinicResponseDto
                            {
                                Id =
                                    clinic.Id,

                                Name =
                                    clinic.Name,

                                Type =
                                    clinic.Type,

                                Address =
                                    clinic.Address,

                                ContactNumber =
                                    clinic.ContactNumber,

                                Latitude =
                                    clinic.Latitude,

                                Longitude =
                                    clinic.Longitude,

                                Services =
                                    clinic.Services,

                                OpeningTime =
                                    clinic.OpeningTime,

                                ClosingTime =
                                    clinic.ClosingTime,

                                IsActive =
                                    clinic.IsActive
                            }
                    )
                    .ToListAsync();

            return Ok(
                clinics
            );
        }

        [HttpGet("clinic-admins")]
        public async Task<IActionResult>
            GetClinicAdmins()
        {
            var admins =
                await _context.Admins
                    .AsNoTracking()
                    .Where(
                        admin =>
                            admin.User.Role ==
                            RoleNames.ClinicAdmin
                    )
                    .OrderBy(
                        admin =>
                            admin.User
                                .FullName
                    )
                    .Select(
                        admin =>
                            new SuperAdminClinicAdminDto
                            {
                                UserId =
                                    admin.UserId,

                                AdminId =
                                    admin.Id,

                                FullName =
                                    admin.User
                                        .FullName,

                                Email =
                                    admin.User
                                        .Email,

                                PhoneNumber =
                                    admin.User
                                        .PhoneNumber,

                                IsActive =
                                    admin.User
                                        .IsActive,

                                ClinicId =
                                    admin.ClinicId,

                                ClinicName =
                                    admin.Clinic !=
                                    null
                                        ? admin
                                            .Clinic
                                            .Name
                                        : null,

                                CreatedAt =
                                    admin.CreatedAt,

                                UpdatedAt =
                                    admin.UpdatedAt
                            }
                    )
                    .ToListAsync();

            return Ok(
                admins
            );
        }

        // =====================================================
        // ASSIGN / REASSIGN CLINIC ADMIN
        // =====================================================

        [HttpPatch(
            "clinic-admins/{userId:guid}/assign"
        )]
        public async Task<IActionResult>
            AssignClinicAdmin(
                Guid userId,
                [FromBody]
                AssignClinicAdminDto dto
            )
        {
            if (
                dto.ClinicId ==
                Guid.Empty
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Select a clinic."
                    }
                );
            }

            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            dto.ClinicId
                    );

            if (
                clinic ==
                null
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            "Clinic not found."
                    }
                );
            }

            if (
                !clinic.IsActive
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "A Clinic Administrator can only be assigned to an active clinic."
                    }
                );
            }

            var admin =
                await _context.Admins
                    .Include(
                        item =>
                            item.User
                    )
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.UserId ==
                                userId &&
                            item.User.Role ==
                                RoleNames.ClinicAdmin
                    );

            if (
                admin ==
                null
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            "Clinic Administrator account not found."
                    }
                );
            }

            var previousClinicId =
                admin.ClinicId;

            var previousClinicName =
                admin.Clinic
                    ?.Name;

            string eventType;
            string auditAction;

            if (
                previousClinicId ==
                null
            )
            {
                eventType =
                    "assigned";

                auditAction =
                    "ClinicAdminAssigned";
            }
            else if (
                previousClinicId ==
                clinic.Id
            )
            {
                eventType =
                    "confirmed";

                auditAction =
                    "ClinicAdminAssignmentConfirmed";
            }
            else
            {
                eventType =
                    "reassigned";

                auditAction =
                    "ClinicAdminReassigned";
            }

            admin.ClinicId =
                clinic.Id;

            admin.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            var currentUserId =
                GetCurrentUserId();

            await _audit
                .LogAsync(
                    auditAction,
                    currentUserId,
                    eventType ==
                        "reassigned"
                        ? $"Clinic Administrator {admin.UserId} was reassigned from {previousClinicName ?? "Unassigned"} to {clinic.Name}."
                        : $"Clinic Administrator {admin.UserId} was assigned to {clinic.Name}.",
                    clinic.Id
                );

            var emailSent =
                await TrySendAssignmentEmailAsync(
                    admin.User.Email,
                    admin.User.FullName,
                    eventType,
                    clinic.Name,
                    previousClinicName
                );

            await _audit
                .LogAsync(
                    emailSent
                        ? "ClinicAdminAssignmentEmailSent"
                        : "ClinicAdminAssignmentEmailDeliveryFailed",
                    currentUserId,
                    emailSent
                        ? $"Clinic assignment notification sent to {admin.User.Email}."
                        : $"Clinic assignment was saved, but notification delivery to {admin.User.Email} failed.",
                    clinic.Id
                );

            var result =
                await GetClinicAdminDtoAsync(
                    userId
                );

            return Ok(
                new
                {
                    result.UserId,
                    result.AdminId,
                    result.FullName,
                    result.Email,
                    result.PhoneNumber,
                    result.IsActive,
                    result.ClinicId,
                    result.ClinicName,
                    result.CreatedAt,
                    result.UpdatedAt,

                    notificationEmailSent =
                        emailSent,

                    notificationEmailMessage =
                        emailSent
                            ? $"Assignment notification sent to {result.Email}."
                            : $"The clinic assignment was saved, but the notification email could not be delivered to {result.Email}."
                }
            );
        }

        // =====================================================
        // DEASSIGN CLINIC ADMIN
        // =====================================================

        [HttpPatch(
            "clinic-admins/{userId:guid}/deassign"
        )]
        public async Task<IActionResult>
            DeassignClinicAdmin(
                Guid userId
            )
        {
            var admin =
                await _context.Admins
                    .Include(
                        item =>
                            item.User
                    )
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.UserId ==
                                userId &&
                            item.User.Role ==
                                RoleNames.ClinicAdmin
                    );

            if (
                admin ==
                null
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            "Clinic Administrator account not found."
                    }
                );
            }

            var previousClinicId =
                admin.ClinicId;

            var previousClinicName =
                admin.Clinic
                    ?.Name;

            if (
                previousClinicId ==
                null
            )
            {
                var alreadyUnassigned =
                    await GetClinicAdminDtoAsync(
                        userId
                    );

                return Ok(
                    new
                    {
                        alreadyUnassigned.UserId,
                        alreadyUnassigned.AdminId,
                        alreadyUnassigned.FullName,
                        alreadyUnassigned.Email,
                        alreadyUnassigned.PhoneNumber,
                        alreadyUnassigned.IsActive,
                        alreadyUnassigned.ClinicId,
                        alreadyUnassigned.ClinicName,
                        alreadyUnassigned.CreatedAt,
                        alreadyUnassigned.UpdatedAt,

                        notificationEmailSent =
                            (bool?)null,

                        notificationEmailMessage =
                            "The Clinic Administrator was already unassigned."
                    }
                );
            }

            admin.ClinicId =
                null;

            admin.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            var currentUserId =
                GetCurrentUserId();

            await _audit
                .LogAsync(
                    "ClinicAdminDeassigned",
                    currentUserId,
                    $"Clinic Administrator {admin.UserId} was deassigned from {previousClinicName ?? "their clinic"}.",
                    previousClinicId
                );

            var emailSent =
                await TrySendAssignmentEmailAsync(
                    admin.User.Email,
                    admin.User.FullName,
                    "deassigned",
                    null,
                    previousClinicName
                );

            await _audit
                .LogAsync(
                    emailSent
                        ? "ClinicAdminDeassignmentEmailSent"
                        : "ClinicAdminDeassignmentEmailDeliveryFailed",
                    currentUserId,
                    emailSent
                        ? $"Clinic deassignment notification sent to {admin.User.Email}."
                        : $"Clinic deassignment was saved, but notification delivery to {admin.User.Email} failed.",
                    previousClinicId
                );

            var result =
                await GetClinicAdminDtoAsync(
                    userId
                );

            return Ok(
                new
                {
                    result.UserId,
                    result.AdminId,
                    result.FullName,
                    result.Email,
                    result.PhoneNumber,
                    result.IsActive,
                    result.ClinicId,
                    result.ClinicName,
                    result.CreatedAt,
                    result.UpdatedAt,

                    notificationEmailSent =
                        emailSent,

                    notificationEmailMessage =
                        emailSent
                            ? $"Deassignment notification sent to {result.Email}."
                            : $"The deassignment was saved, but the notification email could not be delivered to {result.Email}."
                }
            );
        }

        // =====================================================
        // ANALYTICS
        // =====================================================

        [HttpGet("analytics")]
        public async Task<IActionResult>
            Analytics(
                [FromQuery]
                SuperAdminAnalyticsQueryDto query
            )
        {
            try
            {
                var (
                    from,
                    to,
                    endExclusive
                ) =
                    ResolveRange(
                        query.DateFrom,
                        query.DateTo
                    );

                var scopeName =
                    "All clinics";

                if (
                    query.ClinicId
                        .HasValue
                )
                {
                    var clinic =
                        await _context
                            .Clinics
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                item =>
                                    item.Id ==
                                    query.ClinicId
                                        .Value
                            );

                    if (
                        clinic ==
                        null
                    )
                    {
                        return NotFound(
                            new
                            {
                                message =
                                    "Clinic not found."
                            }
                        );
                    }

                    scopeName =
                        clinic.Name;
                }

                var patients =
                    _context.Patients
                        .AsNoTracking()
                        .AsQueryable();

                var nurses =
                    _context.Nurses
                        .AsNoTracking()
                        .AsQueryable();

                var proxies =
                    _context.Proxies
                        .AsNoTracking()
                        .AsQueryable();

                var appointments =
                    _context.Appointments
                        .AsNoTracking()
                        .AsQueryable();

                var collections =
                    _context
                        .MedicationCollections
                        .AsNoTracking()
                        .AsQueryable();

                var medicationLogs =
                    _context.MedicationLogs
                        .AsNoTracking()
                        .AsQueryable();

                var stock =
                    _context.ClinicStocks
                        .AsNoTracking()
                        .AsQueryable();

                if (
                    query.ClinicId
                        .HasValue
                )
                {
                    var clinicId =
                        query.ClinicId
                            .Value;

                    patients =
                        patients.Where(
                            item =>
                                item.ClinicId ==
                                clinicId
                        );

                    nurses =
                        nurses.Where(
                            item =>
                                item.ClinicId ==
                                clinicId
                        );

                    proxies =
                        proxies.Where(
                            item =>
                                item.ClinicId ==
                                clinicId
                        );

                    appointments =
                        appointments.Where(
                            item =>
                                item.ClinicId ==
                                clinicId
                        );

                    collections =
                        collections.Where(
                            item =>
                                item.ClinicId ==
                                clinicId
                        );

                    medicationLogs =
                        medicationLogs.Where(
                            item =>
                                item.Medication
                                    .Patient
                                    .ClinicId ==
                                clinicId
                        );

                    stock =
                        stock.Where(
                            item =>
                                item.ClinicId ==
                                clinicId
                        );
                }

                var periodAppointments =
                    appointments.Where(
                        item =>
                            item.ScheduledAt >=
                                from &&
                            item.ScheduledAt <
                                endExclusive
                    );

                var periodCollections =
                    collections.Where(
                        item =>
                            item.ScheduledCollectionDate >=
                                from &&
                            item.ScheduledCollectionDate <
                                endExclusive
                    );

                var periodMedicationLogs =
                    medicationLogs.Where(
                        item =>
                            item.TakenAt >=
                                from &&
                            item.TakenAt <
                                endExclusive
                    );

                var totalMedicationLogs =
                    await periodMedicationLogs
                        .CountAsync();

                var missedMedicationLogs =
                    await periodMedicationLogs
                        .CountAsync(
                            item =>
                                !item.Taken
                        );

                var takenMedicationLogs =
                    totalMedicationLogs -
                    missedMedicationLogs;

                var clinicQuery =
                    _context.Clinics
                        .AsNoTracking()
                        .AsQueryable();

                if (
                    query.ClinicId
                        .HasValue
                )
                {
                    var clinicId =
                        query.ClinicId
                            .Value;

                    clinicQuery =
                        clinicQuery.Where(
                            item =>
                                item.Id ==
                                clinicId
                        );
                }

                var clinicMetrics =
                    await clinicQuery
                        .Select(
                            clinic =>
                                new SuperAdminClinicMetricDto
                                {
                                    ClinicId =
                                        clinic.Id,

                                    ClinicName =
                                        clinic.Name,

                                    IsActive =
                                        clinic.IsActive,

                                    Patients =
                                        _context.Patients
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                    clinic.Id
                                            ),

                                    Nurses =
                                        _context.Nurses
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                    clinic.Id
                                            ),

                                    Proxies =
                                        _context.Proxies
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                    clinic.Id
                                            ),

                                    ClinicAdmins =
                                        _context.Admins
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                        clinic.Id &&
                                                    item.User.Role ==
                                                        RoleNames.ClinicAdmin
                                            ),

                                    Appointments =
                                        _context.Appointments
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                        clinic.Id &&
                                                    item.ScheduledAt >=
                                                        from &&
                                                    item.ScheduledAt <
                                                        endExclusive
                                            ),

                                    Collections =
                                        _context.MedicationCollections
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                        clinic.Id &&
                                                    item.ScheduledCollectionDate >=
                                                        from &&
                                                    item.ScheduledCollectionDate <
                                                        endExclusive
                                            ),

                                    MissedCollections =
                                        _context.MedicationCollections
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                        clinic.Id &&
                                                    item.ScheduledCollectionDate >=
                                                        from &&
                                                    item.ScheduledCollectionDate <
                                                        endExclusive &&
                                                    item.Status ==
                                                        "Missed"
                                            ),

                                    LowStockItems =
                                        _context.ClinicStocks
                                            .Count(
                                                item =>
                                                    item.ClinicId ==
                                                        clinic.Id &&
                                                    item.IsActive &&
                                                    item.QuantityOnHand <=
                                                        item.ReorderLevel
                                            )
                                }
                        )
                        .OrderByDescending(
                            item =>
                                item.Collections
                        )
                        .ThenBy(
                            item =>
                                item.ClinicName
                        )
                        .ToListAsync();

                var registrationDates =
                    await patients
                        .Where(
                            item =>
                                item.CreatedAt >=
                                    from &&
                                item.CreatedAt <
                                    endExclusive
                        )
                        .Select(
                            item =>
                                item.CreatedAt
                        )
                        .ToListAsync();

                var appointmentDates =
                    await periodAppointments
                        .Select(
                            item =>
                                item.ScheduledAt
                        )
                        .ToListAsync();

                var collectionDates =
                    await periodCollections
                        .Select(
                            item =>
                                item.ScheduledCollectionDate
                        )
                        .ToListAsync();

                var result =
                    new SuperAdminAnalyticsDto
                    {
                        ClinicId =
                            query.ClinicId,

                        ClinicName =
                            scopeName,

                        DateFrom =
                            from,

                        DateTo =
                            to,

                        TotalClinics =
                            query.ClinicId.HasValue
                                ? 1
                                : await _context
                                    .Clinics
                                    .CountAsync(),

                        ActiveClinics =
                            query.ClinicId.HasValue
                                ? clinicMetrics
                                    .Count(
                                        item =>
                                            item.IsActive
                                    )
                                : await _context
                                    .Clinics
                                    .CountAsync(
                                        item =>
                                            item.IsActive
                                    ),

                        TotalPatients =
                            await patients
                                .CountAsync(),

                        ActivePatients =
                            await patients
                                .CountAsync(
                                    item =>
                                        item.User
                                            .IsActive
                                ),

                        TotalNurses =
                            await nurses
                                .CountAsync(),

                        ActiveNurses =
                            await nurses
                                .CountAsync(
                                    item =>
                                        item.User
                                            .IsActive
                                ),

                        TotalProxies =
                            await proxies
                                .CountAsync(),

                        ActiveProxies =
                            await proxies
                                .CountAsync(
                                    item =>
                                        item.User
                                            .IsActive
                                ),

                        Appointments =
                            await periodAppointments
                                .CountAsync(),

                        Collections =
                            await periodCollections
                                .CountAsync(),

                        MissedCollections =
                            await periodCollections
                                .CountAsync(
                                    item =>
                                        item.Status ==
                                        "Missed"
                                ),

                        MedicationLogs =
                            totalMedicationLogs,

                        MissedMedicationLogs =
                            missedMedicationLogs,

                        MedicationAdherenceRate =
                            totalMedicationLogs ==
                            0
                                ? 0
                                : Math.Round(
                                    takenMedicationLogs *
                                    100d /
                                    totalMedicationLogs,
                                    1
                                ),

                        LowStockItems =
                            await stock
                                .CountAsync(
                                    item =>
                                        item.IsActive &&
                                        item.QuantityOnHand <=
                                            item.ReorderLevel
                                ),

                        Clinics =
                            clinicMetrics,

                        Trend =
                            BuildTrend(
                                from,
                                to,
                                registrationDates,
                                appointmentDates,
                                collectionDates
                            )
                    };

                return Ok(
                    result
                );
            }
            catch (
                ArgumentException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private async Task<SuperAdminClinicAdminDto>
            GetClinicAdminDtoAsync(
                Guid userId
            )
        {
            return await _context
                .Admins
                .AsNoTracking()
                .Where(
                    admin =>
                        admin.UserId ==
                            userId &&
                        admin.User.Role ==
                            RoleNames.ClinicAdmin
                )
                .Select(
                    admin =>
                        new SuperAdminClinicAdminDto
                        {
                            UserId =
                                admin.UserId,

                            AdminId =
                                admin.Id,

                            FullName =
                                admin.User
                                    .FullName,

                            Email =
                                admin.User
                                    .Email,

                            PhoneNumber =
                                admin.User
                                    .PhoneNumber,

                            IsActive =
                                admin.User
                                    .IsActive,

                            ClinicId =
                                admin.ClinicId,

                            ClinicName =
                                admin.Clinic !=
                                null
                                    ? admin
                                        .Clinic
                                        .Name
                                    : null,

                            CreatedAt =
                                admin.CreatedAt,

                            UpdatedAt =
                                admin.UpdatedAt
                        }
                )
                .FirstAsync();
        }

        private async Task<bool>
            TrySendAssignmentEmailAsync(
                string email,
                string fullName,
                string eventType,
                string? clinicName,
                string? previousClinicName
            )
        {
            try
            {
                await AccountEmailSender
                    .SendClinicAdminAssignmentAsync(
                        _configuration,
                        _logger,
                        email,
                        fullName,
                        eventType,
                        clinicName,
                        previousClinicName
                    );

                return true;
            }
            catch (
                Exception ex
            )
            {
                _logger.LogWarning(
                    ex,
                    "Clinic Administrator assignment changed but notification email to {Email} could not be delivered.",
                    email
                );

                return false;
            }
        }

        private static (
            DateTime From,
            DateTime To,
            DateTime EndExclusive
        ) ResolveRange(
            DateTime? dateFrom,
            DateTime? dateTo
        )
        {
            var to =
                (
                    dateTo ??
                    DateTime.UtcNow
                )
                .Date;

            var from =
                (
                    dateFrom ??
                    to.AddDays(
                        -29
                    )
                )
                .Date;

            if (
                from >
                to
            )
            {
                throw new ArgumentException(
                    "The From date cannot be after the To date."
                );
            }

            if (
                (
                    to -
                    from
                )
                .TotalDays >
                730
            )
            {
                throw new ArgumentException(
                    "The analytics date range cannot exceed 730 days."
                );
            }

            return (
                DateTime.SpecifyKind(
                    from,
                    DateTimeKind.Utc
                ),

                DateTime.SpecifyKind(
                    to,
                    DateTimeKind.Utc
                ),

                DateTime.SpecifyKind(
                    to.AddDays(
                        1
                    ),
                    DateTimeKind.Utc
                )
            );
        }

        private static List<SuperAdminTrendPointDto>
            BuildTrend(
                DateTime from,
                DateTime to,
                IReadOnlyCollection<DateTime>
                    registrations,
                IReadOnlyCollection<DateTime>
                    appointments,
                IReadOnlyCollection<DateTime>
                    collections
            )
        {
            var monthly =
                (
                    to -
                    from
                )
                .TotalDays >
                90;

            string Key(
                DateTime value
            )
            {
                return monthly
                    ? value.ToString(
                        "yyyy-MM"
                    )
                    : value.ToString(
                        "yyyy-MM-dd"
                    );
            }

            var registrationMap =
                registrations
                    .GroupBy(
                        Key
                    )
                    .ToDictionary(
                        group =>
                            group.Key,

                        group =>
                            group.Count()
                    );

            var appointmentMap =
                appointments
                    .GroupBy(
                        Key
                    )
                    .ToDictionary(
                        group =>
                            group.Key,

                        group =>
                            group.Count()
                    );

            var collectionMap =
                collections
                    .GroupBy(
                        Key
                    )
                    .ToDictionary(
                        group =>
                            group.Key,

                        group =>
                            group.Count()
                    );

            var result =
                new List<SuperAdminTrendPointDto>();

            if (
                monthly
            )
            {
                var cursor =
                    new DateTime(
                        from.Year,
                        from.Month,
                        1
                    );

                var end =
                    new DateTime(
                        to.Year,
                        to.Month,
                        1
                    );

                while (
                    cursor <=
                    end
                )
                {
                    var key =
                        cursor.ToString(
                            "yyyy-MM"
                        );

                    result.Add(
                        new SuperAdminTrendPointDto
                        {
                            Period =
                                cursor.ToString(
                                    "MMM yy"
                                ),

                            Registrations =
                                registrationMap
                                    .GetValueOrDefault(
                                        key
                                    ),

                            Appointments =
                                appointmentMap
                                    .GetValueOrDefault(
                                        key
                                    ),

                            Collections =
                                collectionMap
                                    .GetValueOrDefault(
                                        key
                                    )
                        }
                    );

                    cursor =
                        cursor.AddMonths(
                            1
                        );
                }
            }
            else
            {
                for (
                    var cursor =
                        from.Date;
                    cursor <=
                        to.Date;
                    cursor =
                        cursor.AddDays(
                            1
                        )
                )
                {
                    var key =
                        cursor.ToString(
                            "yyyy-MM-dd"
                        );

                    result.Add(
                        new SuperAdminTrendPointDto
                        {
                            Period =
                                cursor.ToString(
                                    "dd MMM"
                                ),

                            Registrations =
                                registrationMap
                                    .GetValueOrDefault(
                                        key
                                    ),

                            Appointments =
                                appointmentMap
                                    .GetValueOrDefault(
                                        key
                                    ),

                            Collections =
                                collectionMap
                                    .GetValueOrDefault(
                                        key
                                    )
                        }
                    );
                }
            }

            return result;
        }

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
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
                throw new UnauthorizedAccessException(
                    "Invalid authentication token."
                );
            }

            return userId;
        }
    }
}
