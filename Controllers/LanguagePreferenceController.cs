using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Localization;
using PersonalProject.Models.Constants;
using PersonalProject.Models.Entities;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route(
        "api/patients/me/language"
    )]
    [Authorize(
        Policy = "PatientOnly"
    )]
    public class
        LanguagePreferenceController :
        ControllerBase
    {
        private readonly
            PhilaLinkDbContext
            _context;

        public LanguagePreferenceController(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetLanguage()
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

            var normalized =
                PhilaLinkLocalization
                    .Normalize(
                        preference
                            .PreferredLanguage
                    );

            /*
             * Repair legacy or invalid values.
             */
            if (
                !string.Equals(
                    preference
                        .PreferredLanguage,
                    normalized,
                    StringComparison
                        .Ordinal
                )
            )
            {
                preference
                    .PreferredLanguage =
                    normalized;

                preference.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();
            }

            return Ok(
                new
                {
                    language =
                        normalized,

                    supportedLanguages =
                        PhilaLinkLocalization
                            .SupportedLanguages
                }
            );
        }

        // =====================================================
        // UPDATE
        // =====================================================

        [HttpPut]
        public async Task<IActionResult>
            UpdateLanguage(
                LanguagePreferenceRequest
                    request
            )
        {
            var language =
                request.Language
                    ?.Trim()
                    .ToLowerInvariant();

            if (
                !PhilaLinkLocalization
                    .IsSupported(
                        language
                    )
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Unsupported language.",

                        supportedLanguages =
                            PhilaLinkLocalization
                                .SupportedLanguages
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
                    preference
                        .PreferredLanguage,
                    language,
                    StringComparison.Ordinal
                )
            )
            {
                preference
                    .PreferredLanguage =
                    language!;

                preference.UpdatedAt =
                    DateTime.UtcNow;

                await _context
                    .SaveChangesAsync();
            }

            return Ok(
                new
                {
                    language =
                        preference
                            .PreferredLanguage
                }
            );
        }

        // =====================================================
        // PATIENT RESOLUTION
        // =====================================================

        private async Task<Guid?>
            GetPatientIdAsync(
                Guid userId
            )
        {
            return await _context
                .Patients
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

                    PreferredLanguage =
                        PhilaLinkLocalization
                            .DefaultLanguage,

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
        // AUTH USER
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
        LanguagePreferenceRequest
    {
        public string Language
        {
            get;
            set;
        } = string.Empty;
    }
}
