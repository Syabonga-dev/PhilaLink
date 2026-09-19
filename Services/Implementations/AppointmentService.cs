using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class AppointmentService :
        IAppointmentService
    {
        private static readonly TimeZoneInfo
            SouthAfricaTimeZone =
                ResolveSouthAfricaTimeZone();

        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        private readonly INotificationService
            _notificationService;

        private readonly ILogger<
            AppointmentService
        > _logger;

        public AppointmentService(
            PhilaLinkDbContext context,
            IAuditLogService audit,
            INotificationService notificationService,
            ILogger<AppointmentService> logger
        )
        {
            _context =
                context;

            _audit =
                audit;

            _notificationService =
                notificationService;

            _logger =
                logger;
        }

        // =====================================================
        // CREATE
        // =====================================================

        public async Task<
            AppointmentResponseDto
        > CreateAsync(
            CreateAppointmentDto dto,
            Guid performedByUserId
        )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            if (
                dto.ClinicId !=
                    clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You can only create appointments for your clinic."
                );
            }

            if (
                dto.ScheduledAt <=
                    DateTime.UtcNow
            )
            {
                throw new InvalidOperationException(
                    "Appointment date must be in the future."
                );
            }

            if (
                dto.DurationMinutes <=
                    0
            )
            {
                throw new InvalidOperationException(
                    "Appointment duration must be greater than zero."
                );
            }

            var patient =
                await _context.Patients
                    .Include(
                        patient =>
                            patient.User
                    )
                    .FirstOrDefaultAsync(
                        patient =>
                            patient.Id ==
                                dto.PatientId
                    );

            if (
                patient ==
                    null
            )
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (
                patient.ClinicId !=
                    clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "The patient does not belong to your clinic."
                );
            }

            await ValidateNurseAsync(
                dto.NurseId,
                clinicId
            );

            var appointment =
                new Appointment
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        dto.PatientId,

                    ClinicId =
                        clinicId,

                    NurseId =
                        dto.NurseId,

                    ScheduledAt =
                        dto.ScheduledAt,

                    DurationMinutes =
                        dto.DurationMinutes,

                    Type =
                        dto.Type.Trim(),

                    Reason =
                        dto.Reason.Trim(),

                    ProviderName =
                        string.IsNullOrWhiteSpace(
                            dto.ProviderName
                        )
                            ? null
                            : dto
                                .ProviderName
                                .Trim(),

                    Mode =
                        AppointmentModes
                            .Normalize(
                                dto.Mode
                            ),

                    Status =
                        AppointmentStatuses
                            .Scheduled,

                    Notes =
                        dto.Notes,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Appointments.Add(
                appointment
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "AppointmentCreated",
                performedByUserId,
                $"Appointment {appointment.Id} created " +
                $"for patient {patient.Id}."
            );

            var result =
                (
                    await GetAppointmentQuery()
                        .FirstAsync(
                            item =>
                                item.Id ==
                                    appointment.Id
                        )
                )
                .ToDto();

            await TryNotifyAppointmentAsync(
                patient.UserId,
                result
            );

            return result;
        }

        // =====================================================
        // UPDATE
        // Includes confirmation / approval, rescheduling,
        // cancellation, completion and other status changes.
        // =====================================================

        public async Task<
            AppointmentResponseDto
        > UpdateAsync(
            Guid appointmentId,
            UpdateAppointmentDto dto,
            Guid performedByUserId
        )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var appointment =
                await GetAppointmentQuery()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                appointmentId
                    );

            if (
                appointment ==
                    null
            )
            {
                throw new KeyNotFoundException(
                    "Appointment not found."
                );
            }

            if (
                appointment.ClinicId !=
                    clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot manage appointments from another clinic."
                );
            }

            if (
                dto.DurationMinutes <=
                    0
            )
            {
                throw new InvalidOperationException(
                    "Appointment duration must be greater than zero."
                );
            }

            await ValidateNurseAsync(
                dto.NurseId,
                clinicId
            );

            appointment.ScheduledAt =
                dto.ScheduledAt;

            appointment.DurationMinutes =
                dto.DurationMinutes;

            appointment.NurseId =
                dto.NurseId;

            appointment.Type =
                dto.Type.Trim();

            appointment.Reason =
                dto.Reason.Trim();

            appointment.ProviderName =
                string.IsNullOrWhiteSpace(
                    dto.ProviderName
                )
                    ? null
                    : dto
                        .ProviderName
                        .Trim();

            appointment.Mode =
                NormalizeMode(
                    dto.Mode
                );

            appointment.Status =
                AppointmentStatuses
                    .Normalize(
                        dto.Status
                    );

            appointment.Notes =
                dto.Notes;

            appointment.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "AppointmentUpdated",
                performedByUserId,
                $"Appointment {appointment.Id} updated."
            );

            var result =
                appointment
                    .ToDto();

            await TryNotifyAppointmentAsync(
                appointment
                    .Patient
                    .UserId,
                result
            );

            return result;
        }

        // =====================================================
        // DELETE
        // =====================================================

        public async Task
            DeleteAsync(
                Guid appointmentId,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var appointment =
                await GetAppointmentQuery()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                appointmentId
                    );

            if (
                appointment ==
                    null
            )
            {
                throw new KeyNotFoundException(
                    "Appointment not found."
                );
            }

            if (
                appointment.ClinicId !=
                    clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You cannot manage appointments from another clinic."
                );
            }

            var patientUserId =
                appointment
                    .Patient
                    .UserId;

            var snapshot =
                appointment
                    .ToDto();

            _context.Appointments.Remove(
                appointment
            );

            await _context
                .SaveChangesAsync();

            await _audit.LogAsync(
                "AppointmentDeleted",
                performedByUserId,
                $"Appointment {appointmentId} deleted."
            );

            await TryNotifyAppointmentAsync(
                patientUserId,
                snapshot,
                "Deleted"
            );
        }

        // =====================================================
        // GET ONE
        // =====================================================

        public async Task<
            AppointmentResponseDto?
        > GetByIdAsync(
            Guid appointmentId,
            Guid performedByUserId
        )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var appointment =
                await GetAppointmentQuery()
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                appointmentId &&
                            item.ClinicId ==
                                clinicId
                    );

            return appointment
                ?.ToDto();
        }

        // =====================================================
        // CLINIC APPOINTMENTS
        // =====================================================

        public async Task<
            List<AppointmentResponseDto>
        > GetClinicAppointmentsAsync(
            Guid performedByUserId
        )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var appointments =
                await GetAppointmentQuery()
                    .Where(
                        item =>
                            item.ClinicId ==
                                clinicId
                    )
                    .OrderBy(
                        item =>
                            item.ScheduledAt
                    )
                    .ToListAsync();

            return appointments
                .Select(
                    item =>
                        item.ToDto()
                )
                .ToList();
        }

        // =====================================================
        // PATIENT APPOINTMENTS
        // =====================================================

        public async Task<
            List<AppointmentResponseDto>
        > GetPatientAppointmentsAsync(
            Guid patientUserId
        )
        {
            var patient =
                await _context.Patients
                    .Include(
                        item =>
                            item.User
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.UserId ==
                                patientUserId &&
                            item.User.Role ==
                                RoleNames.Patient &&
                            item.User.IsActive
                    );

            if (
                patient ==
                    null
            )
            {
                throw new KeyNotFoundException(
                    "Active patient profile not found."
                );
            }

            var appointments =
                await GetAppointmentQuery()
                    .Where(
                        item =>
                            item.PatientId ==
                                patient.Id
                    )
                    .OrderBy(
                        item =>
                            item.ScheduledAt
                    )
                    .ToListAsync();

            return appointments
                .Select(
                    item =>
                        item.ToDto()
                )
                .ToList();
        }

        // =====================================================
        // APPOINTMENT NOTIFICATION
        // =====================================================

        private async Task
            TryNotifyAppointmentAsync(
                Guid patientUserId,
                AppointmentResponseDto
                    appointment,
                string? statusOverride =
                    null
            )
        {
            try
            {
                var localTime =
                    ToSouthAfricaTime(
                        appointment
                            .ScheduledAt
                    );

                var type =
                    string.IsNullOrWhiteSpace(
                        appointment.Type
                    )
                        ? "appointment"
                        : appointment.Type;

                var status =
                    string.IsNullOrWhiteSpace(
                        statusOverride
                    )
                        ? appointment.Status
                        : statusOverride;

                /*
                 * This deliberately matches the message created
                 * by MyNotificationsController. That prevents
                 * the later polling pass from creating a
                 * duplicate notification.
                 */
                var message =
                    "Appointment update: " +
                    $"Your {type} at " +
                    $"{appointment.ClinicName} is " +
                    $"{status} for " +
                    $"{localTime:ddd, dd MMM yyyy 'at' HH:mm}.";

                await _notificationService
                    .CreateAppointmentForPatientAsync(
                        patientUserId,
                        message
                    );
            }
            catch (
                Exception ex
            )
            {
                /*
                 * Appointment changes must not be rolled back
                 * solely because a secondary notification could
                 * not be created.
                 */
                _logger.LogWarning(
                    ex,
                    "Appointment {AppointmentId} was saved, but its patient notification could not be created.",
                    appointment.Id
                );
            }
        }

        // =====================================================
        // QUERY
        // =====================================================

        private IQueryable<Appointment>
            GetAppointmentQuery()
        {
            return _context.Appointments
                .Include(
                    appointment =>
                        appointment.Patient
                )
                .ThenInclude(
                    patient =>
                        patient.User
                )
                .Include(
                    appointment =>
                        appointment.Clinic
                )
                .Include(
                    appointment =>
                        appointment.Nurse
                )
                .ThenInclude(
                    nurse =>
                        nurse!.User
                );
        }

        // =====================================================
        // STAFF CLINIC
        // =====================================================

        private async Task<Guid>
            GetStaffClinicIdAsync(
                Guid userId
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
                    null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException(
                    "Active staff account required."
                );
            }

            if (
                user.Role ==
                    RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId !=
                    null
            )
            {
                return user
                    .Admin
                    .ClinicId
                    .Value;
            }

            if (
                user.Role ==
                    RoleNames.Nurse &&
                user.Nurse !=
                    null
            )
            {
                return user
                    .Nurse
                    .ClinicId;
            }

            throw new UnauthorizedAccessException(
                "Clinic staff privileges are required."
            );
        }

        // =====================================================
        // NURSE VALIDATION
        // =====================================================

        private async Task
            ValidateNurseAsync(
                Guid? nurseId,
                Guid clinicId
            )
        {
            if (
                nurseId ==
                    null
            )
            {
                return;
            }

            var nurse =
                await _context.Nurses
                    .Include(
                        item =>
                            item.User
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                nurseId
                    );

            if (
                nurse ==
                    null
            )
            {
                throw new KeyNotFoundException(
                    "Assigned nurse not found."
                );
            }

            if (
                nurse.ClinicId !=
                    clinicId ||
                !nurse.User.IsActive
            )
            {
                throw new InvalidOperationException(
                    "Assigned nurse must be an active nurse at the appointment clinic."
                );
            }
        }

        // =====================================================
        // MODE
        // =====================================================

        private static string
            NormalizeMode(
                string mode
            )
        {
            if (
                string.Equals(
                    mode,
                    "Telehealth",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return
                    "Telehealth";
            }

            return
                "InPerson";
        }

        // =====================================================
        // TIMEZONE
        // =====================================================

        private static TimeZoneInfo
            ResolveSouthAfricaTimeZone()
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Africa/Johannesburg"
                    );
            }
            catch (
                TimeZoneNotFoundException
            )
            {
                try
                {
                    return TimeZoneInfo
                        .FindSystemTimeZoneById(
                            "South Africa Standard Time"
                        );
                }
                catch
                {
                    return TimeZoneInfo
                        .CreateCustomTimeZone(
                            "PhilaLink-SAST",
                            TimeSpan
                                .FromHours(2),
                            "South Africa Standard Time",
                            "South Africa Standard Time"
                        );
                }
            }
        }

        private static DateTime
            ToSouthAfricaTime(
                DateTime value
            )
        {
            var utc =
                value.Kind ==
                    DateTimeKind.Utc
                    ? value
                    : DateTime
                        .SpecifyKind(
                            value,
                            DateTimeKind.Utc
                        );

            return TimeZoneInfo
                .ConvertTimeFromUtc(
                    utc,
                    SouthAfricaTimeZone
                );
        }
    }

    internal static class
        AppointmentMappings
    {
        public static AppointmentResponseDto
            ToDto(
                this Appointment
                    appointment
            )
        {
            return new AppointmentResponseDto
            {
                Id =
                    appointment.Id,

                PatientId =
                    appointment.PatientId,

                PatientName =
                    appointment
                        .Patient
                        .User
                        .FullName,

                ClinicId =
                    appointment.ClinicId,

                ClinicName =
                    appointment
                        .Clinic
                        .Name,

                NurseId =
                    appointment.NurseId,

                NurseName =
                    appointment.Nurse?
                        .User
                        .FullName,

                ScheduledAt =
                    appointment.ScheduledAt,

                DurationMinutes =
                    appointment.DurationMinutes,

                Type =
                    appointment.Type,

                Reason =
                    appointment.Reason,

                ProviderName =
                    appointment.ProviderName,

                Mode =
                    appointment.Mode,

                Status =
                    appointment.Status,

                Notes =
                    appointment.Notes
            };
        }
    }
}
