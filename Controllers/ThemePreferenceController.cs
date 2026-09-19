using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/patients/me/theme")]
    [Authorize(Policy = "PatientOnly")]
    public class ThemePreferenceController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        public ThemePreferenceController(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // GET CURRENT THEME
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetTheme()
        {
            var patientId =
                await GetPatientIdAsync(
                    GetCurrentUserId()
                );

            if (
                patientId ==
                    null
            )
            {
                return Forbid();
            }

            var preference =
                await GetOrCreatePreferenceAsync(
                    patientId.Value
                );

            var theme =
                NormalizeStoredTheme(
                    preference.Theme
                );

            /*
             * Repair any old/invalid value automatically.
             */
            if (
                !string.Equals(
                    preference.Theme,
                    theme,
                    StringComparison.Ordinal
                )
            )
            {
                preference.Theme =
                    theme;

                preference.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();
            }

            return Ok(
                new
                {
                    theme
                }
            );
        }

        // =====================================================
        // UPDATE THEME
        // =====================================================

        [HttpPut]
        public async Task<IActionResult>
            UpdateTheme(
                ThemePreferenceRequest dto
            )
        {
            var theme =
                NormalizeRequestedTheme(
                    dto.Theme
                );

            if (
                theme ==
                    null
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Theme must be either 'light' or 'dark'."
                    }
                );
            }

            var patientId =
                await GetPatientIdAsync(
                    GetCurrentUserId()
                );

            if (
                patientId ==
                    null
            )
            {
                return Forbid();
            }

            var preference =
                await GetOrCreatePreferenceAsync(
                    patientId.Value
                );

            if (
                !string.Equals(
                    preference.Theme,
                    theme,
                    StringComparison.Ordinal
                )
            )
            {
                preference.Theme =
                    theme;

                preference.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();
            }

            return Ok(
                new
                {
                    theme =
                        preference.Theme
                }
            );
        }

        // =====================================================
        // PATIENT
        // =====================================================

        private async Task<Guid?>
            GetPatientIdAsync(
                Guid userId
            )
        {
            return await _context.Patients
                .AsNoTracking()
                .Where(
                    patient =>
                        patient.UserId ==
                            userId &&
                        patient.User.Role ==
                            RoleNames.Patient &&
                        patient.User.IsActive
                )
                .Select(
                    patient =>
                        (Guid?)patient.Id
                )
                .FirstOrDefaultAsync();
        }

        // =====================================================
        // PREFERENCE
        // =====================================================

        private async Task<PatientPreference>
            GetOrCreatePreferenceAsync(
                Guid patientId
            )
        {
            var preference =
                await _context
                    .PatientPreferences
                    .FirstOrDefaultAsync(
                        item =>
                            item.PatientId ==
                                patientId
                    );

            if (
                preference !=
                    null
            )
            {
                return preference;
            }

            preference =
                new PatientPreference
                {
                    Id =
                        Guid.NewGuid(),

                    PatientId =
                        patientId,

                    MedicationReminders =
                        true,

                    AppointmentReminders =
                        true,

                    ClinicNotifications =
                        true,

                    HealthUpdates =
                        false,

                    ShareHealthData =
                        true,

                    AllowChatbotProfileAccess =
                        true,

                    Theme =
                        "light",

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context
                .PatientPreferences
                .Add(
                    preference
                );

            await _context
                .SaveChangesAsync();

            return preference;
        }

        // =====================================================
        // THEME VALIDATION
        // =====================================================

        private static string?
            NormalizeRequestedTheme(
                string? theme
            )
        {
            if (
                string.Equals(
                    theme,
                    "light",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return "light";
            }

            if (
                string.Equals(
                    theme,
                    "dark",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                return "dark";
            }

            return null;
        }

        private static string
            NormalizeStoredTheme(
                string? theme
            )
        {
            return string.Equals(
                theme,
                "dark",
                StringComparison
                    .OrdinalIgnoreCase
            )
                ? "dark"
                : "light";
        }

        // =====================================================
        // CURRENT USER
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
                throw new
                    UnauthorizedAccessException(
                        "Invalid authentication token."
                    );
            }

            return userId;
        }
    }

    public sealed class
        ThemePreferenceRequest
    {
        public string Theme
        {
            get;
            set;
        } = string.Empty;
    }
}
