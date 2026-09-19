using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize(
        Policy = "PatientOnly"
    )]
    public class PatientController :
        ControllerBase
    {
        private static readonly TimeZoneInfo
            SouthAfricaTimeZone =
                ResolveSouthAfricaTimeZone();

        private readonly IPatientService
            _patientService;

        private readonly INotificationService
            _notificationService;

        private readonly ILogger<
            PatientController
        > _logger;

        public PatientController(
            IPatientService patientService,
            INotificationService notificationService,
            ILogger<PatientController> logger
        )
        {
            _patientService =
                patientService;

            _notificationService =
                notificationService;

            _logger =
                logger;
        }

        // =====================================================
        // PROFILE
        // =====================================================

        [HttpGet("me")]
        public async Task<IActionResult>
            GetMe()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetMeAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPut("me")]
        public async Task<IActionResult>
            UpdateMe(
                UpdatePatientProfileDto dto
            )
        {
            try
            {
                return Ok(
                    await _patientService
                        .UpdateMeAsync(
                            GetCurrentUserId(),
                            dto
                        )
                );
            }
            catch (
                InvalidOperationException ex
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
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // MEDICATION LOG
        // =====================================================

        [HttpPost(
            "me/medications/{medicationId:guid}/log"
        )]
        public async Task<IActionResult>
            LogMedication(
                Guid medicationId,
                PatientMedicationLogDto dto
            )
        {
            try
            {
                await _patientService
                    .LogMedicationAsync(
                        GetCurrentUserId(),
                        medicationId,
                        dto.Taken,
                        dto.Notes
                    );

                return Ok(
                    new
                    {
                        message =
                            dto.Taken
                                ? "Medication marked as taken."
                                : "Medication marked as skipped."
                    }
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                InvalidOperationException ex
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
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet("me/dashboard")]
        public async Task<IActionResult>
            GetDashboard()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetDashboardAsync(
                            GetCurrentUserId()
                        )
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
        // MEDICATIONS
        // =====================================================

        [HttpGet("me/medications")]
        public async Task<IActionResult>
            GetMedications()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetMedicationsAsync(
                            GetCurrentUserId()
                        )
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
        // APPOINTMENTS
        // =====================================================

        [HttpGet("me/appointments")]
        public async Task<IActionResult>
            GetAppointments()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetAppointmentsAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPost("me/appointments")]
        public async Task<IActionResult>
            BookAppointment(
                PatientBookAppointmentDto dto
            )
        {
            try
            {
                var userId =
                    GetCurrentUserId();

                var result =
                    await _patientService
                        .BookAppointmentAsync(
                            userId,
                            dto
                        );

                await TryNotifyAppointmentAsync(
                    userId,
                    result
                );

                return Ok(
                    result
                );
            }
            catch (
                InvalidOperationException ex
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
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPatch(
            "me/appointments/{id:guid}/reschedule"
        )]
        public async Task<IActionResult>
            RescheduleAppointment(
                Guid id,
                PatientRescheduleAppointmentDto
                    dto
            )
        {
            try
            {
                var userId =
                    GetCurrentUserId();

                var result =
                    await _patientService
                        .RescheduleAppointmentAsync(
                            userId,
                            id,
                            dto
                        );

                await TryNotifyAppointmentAsync(
                    userId,
                    result
                );

                return Ok(
                    result
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                InvalidOperationException ex
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
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPatch(
            "me/appointments/{id:guid}/cancel"
        )]
        public async Task<IActionResult>
            CancelAppointment(
                Guid id
            )
        {
            try
            {
                var userId =
                    GetCurrentUserId();

                await _patientService
                    .CancelAppointmentAsync(
                        userId,
                        id
                    );

                /*
                 * The appointment remains in the database with
                 * Cancelled status, so retrieve its final state
                 * for the notification.
                 */
                var appointments =
                    await _patientService
                        .GetAppointmentsAsync(
                            userId
                        );

                var appointment =
                    appointments
                        .FirstOrDefault(
                            item =>
                                item.Id ==
                                    id
                        );

                if (
                    appointment !=
                        null
                )
                {
                    await TryNotifyAppointmentAsync(
                        userId,
                        appointment
                    );
                }

                return Ok(
                    new
                    {
                        message =
                            "Appointment cancelled."
                    }
                );
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
                    }
                );
            }
            catch (
                InvalidOperationException ex
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
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // RECORDS
        // =====================================================

        [HttpGet("me/records")]
        public async Task<IActionResult>
            GetRecords()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetRecordsAsync(
                            GetCurrentUserId()
                        )
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
        // COLLECTIONS
        // =====================================================

        [HttpGet("me/collections")]
        public async Task<IActionResult>
            GetCollections()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetCollectionsAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpGet(
            "me/collections/next"
        )]
        public async Task<IActionResult>
            GetNextCollection()
        {
            try
            {
                var result =
                    await _patientService
                        .GetNextCollectionAsync(
                            GetCurrentUserId()
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
        // NOTIFICATIONS
        // =====================================================

        [HttpGet(
            "me/notifications"
        )]
        public async Task<IActionResult>
            GetNotifications()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetNotificationsAsync(
                            GetCurrentUserId()
                        )
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
            "me/notifications/{id:guid}/read"
        )]
        public async Task<IActionResult>
            MarkNotificationRead(
                Guid id
            )
        {
            try
            {
                await _patientService
                    .MarkNotificationReadAsync(
                        GetCurrentUserId(),
                        id
                    );

                return NoContent();
            }
            catch (
                KeyNotFoundException ex
            )
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message
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
        // SETTINGS / PREFERENCES
        // =====================================================

        [HttpGet(
            "me/preferences"
        )]
        public async Task<IActionResult>
            GetPreferences()
        {
            try
            {
                return Ok(
                    await _patientService
                        .GetPreferencesAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPut(
            "me/preferences"
        )]
        public async Task<IActionResult>
            UpdatePreferences(
                PatientPreferenceDto dto
            )
        {
            try
            {
                return Ok(
                    await _patientService
                        .UpdatePreferencesAsync(
                            GetCurrentUserId(),
                            dto
                        )
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
        // APPOINTMENT NOTIFICATION
        // =====================================================

        private async Task
            TryNotifyAppointmentAsync(
                Guid userId,
                AppointmentResponseDto
                    appointment
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

                var message =
                    "Appointment update: " +
                    $"Your {type} at " +
                    $"{appointment.ClinicName} is " +
                    $"{appointment.Status} for " +
                    $"{localTime:ddd, dd MMM yyyy 'at' HH:mm}.";

                await _notificationService
                    .CreateAppointmentForPatientAsync(
                        userId,
                        message
                    );
            }
            catch (
                Exception ex
            )
            {
                /*
                 * Do not turn a successfully saved appointment
                 * into an API failure merely because its
                 * notification failed.
                 */
                _logger.LogWarning(
                    ex,
                    "Appointment {AppointmentId} was saved, but its notification could not be created.",
                    appointment.Id
                );
            }
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

        // =====================================================
        // JWT USER
        // =====================================================

        private Guid
            GetCurrentUserId()
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
                throw new UnauthorizedAccessException(
                    "Invalid authentication token."
                );
            }

            return userId;
        }
    }
}
