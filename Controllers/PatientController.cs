using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize(Policy = "PatientOnly")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // =====================================================
        // PROFILE
        // =====================================================

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                return Ok(await _patientService.GetMeAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdatePatientProfileDto dto)
        {
            try
            {
                return Ok(await _patientService.UpdateMeAsync(GetCurrentUserId(), dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet("me/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                return Ok(await _patientService.GetDashboardAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // MEDICATIONS
        // =====================================================

        [HttpGet("me/medications")]
        public async Task<IActionResult> GetMedications()
        {
            try
            {
                return Ok(await _patientService.GetMedicationsAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // APPOINTMENTS
        // =====================================================

        [HttpGet("me/appointments")]
        public async Task<IActionResult> GetAppointments()
        {
            try
            {
                return Ok(await _patientService.GetAppointmentsAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("me/appointments")]
        public async Task<IActionResult> BookAppointment(PatientBookAppointmentDto dto)
        {
            try
            {
                return Ok(await _patientService.BookAppointmentAsync(GetCurrentUserId(), dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPatch("me/appointments/{id:guid}/reschedule")]
        public async Task<IActionResult> RescheduleAppointment(Guid id, PatientRescheduleAppointmentDto dto)
        {
            try
            {
                return Ok(await _patientService.RescheduleAppointmentAsync(GetCurrentUserId(), id, dto));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPatch("me/appointments/{id:guid}/cancel")]
        public async Task<IActionResult> CancelAppointment(Guid id)
        {
            try
            {
                await _patientService.CancelAppointmentAsync(GetCurrentUserId(), id);

                return Ok(new { message = "Appointment cancelled." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // RECORDS
        // =====================================================

        [HttpGet("me/records")]
        public async Task<IActionResult> GetRecords()
        {
            try
            {
                return Ok(await _patientService.GetRecordsAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // COLLECTIONS
        // =====================================================

        [HttpGet("me/collections")]
        public async Task<IActionResult> GetCollections()
        {
            try
            {
                return Ok(await _patientService.GetCollectionsAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("me/collections/next")]
        public async Task<IActionResult> GetNextCollection()
        {
            try
            {
                var result = await _patientService.GetNextCollectionAsync(GetCurrentUserId());

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // NOTIFICATIONS
        // =====================================================

        [HttpGet("me/notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                return Ok(await _patientService.GetNotificationsAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPatch("me/notifications/{id:guid}/read")]
        public async Task<IActionResult> MarkNotificationRead(Guid id)
        {
            try
            {
                await _patientService.MarkNotificationReadAsync(GetCurrentUserId(), id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // SETTINGS / PREFERENCES
        // =====================================================

        [HttpGet("me/preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            try
            {
                return Ok(await _patientService.GetPreferencesAsync(GetCurrentUserId()));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("me/preferences")]
        public async Task<IActionResult> UpdatePreferences(PatientPreferenceDto dto)
        {
            try
            {
                return Ok(await _patientService.UpdatePreferencesAsync(GetCurrentUserId(), dto));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // JWT USER
        // =====================================================

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrWhiteSpace(value) ||
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