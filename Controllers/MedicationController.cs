using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/medications")]
    [Authorize]
    public class MedicationController :
        ControllerBase
    {
        private readonly IMedicationService
            _medicationService;

        private readonly PhilaLinkDbContext
            _context;

        public MedicationController(
            IMedicationService medicationService,
            PhilaLinkDbContext context
        )
        {
            _medicationService =
                medicationService;

            _context =
                context;
        }

        // =====================================================
        // CLINIC STAFF: CREATE MEDICATION
        // =====================================================

        [HttpPost]
        [Authorize(
            Roles = RoleNames.Nurse
        )]
        public async Task<IActionResult>
            Create(
                MedicationCreateDto dto
            )
        {
            try
            {
                var medication =
                    await _medicationService
                        .CreateMedicationAsync(
                            dto,
                            GetCurrentUserId()
                        );

                return Ok(
                    ToMedicationResponse(
                        medication
                    )
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
        // CLINIC STAFF: PATIENT MEDICATIONS
        // =====================================================

        [HttpGet(
            "patient/{patientId:guid}"
        )]
        [Authorize(
            Policy = "ClinicStaff"
        )]
        public async Task<IActionResult>
            GetByPatient(
                Guid patientId
            )
        {
            try
            {
                var shareHealthData =
                    await GetHealthDataSharingSettingAsync(
                        patientId
                    );

                if (
                    shareHealthData ==
                        false
                )
                {
                    return StatusCode(
                        403,
                        new
                        {
                            message =
                                "The patient has disabled health-data sharing."
                        }
                    );
                }

                var medications =
                    await _medicationService
                        .GetPatientMedicationsAsync(
                            patientId,
                            GetCurrentUserId()
                        );

                return Ok(
                    medications.Select(
                        ToMedicationResponse
                    )
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
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // PATIENT: OWN MEDICATIONS
        // =====================================================

        [HttpGet("me")]
        [Authorize(
            Roles = RoleNames.Patient
        )]
        public async Task<IActionResult>
            GetMyMedications()
        {
            try
            {
                var medications =
                    await _medicationService
                        .GetPatientMedicationsByUserIdAsync(
                            GetCurrentUserId()
                        );

                return Ok(
                    medications.Select(
                        ToMedicationResponse
                    )
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
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // CLINIC STAFF: ADD SCHEDULE
        // =====================================================

        [HttpPost(
            "{medicationId:guid}/schedule"
        )]
        [Authorize(
            Roles = RoleNames.Nurse
        )]
        public async Task<IActionResult>
            AddSchedule(
                Guid medicationId,
                MedicationScheduleDto dto
            )
        {
            try
            {
                await _medicationService
                    .AddScheduleAsync(
                        medicationId,
                        dto.TimeOfDay,
                        GetCurrentUserId()
                    );

                return Ok(
                    new
                    {
                        message =
                            "Schedule added successfully."
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
        // CLINIC STAFF: MEDICATION LOGS
        // =====================================================

        [HttpGet(
            "{medicationId:guid}/logs"
        )]
        [Authorize(
            Policy = "ClinicStaff"
        )]
        public async Task<IActionResult>
            GetLogs(
                Guid medicationId
            )
        {
            try
            {
                var shareHealthData =
                    await GetMedicationHealthDataSharingSettingAsync(
                        medicationId
                    );

                if (
                    shareHealthData ==
                        false
                )
                {
                    return StatusCode(
                        403,
                        new
                        {
                            message =
                                "The patient has disabled health-data sharing."
                        }
                    );
                }

                var logs =
                    await _medicationService
                        .GetMedicationLogsAsync(
                            medicationId,
                            GetCurrentUserId()
                        );

                return Ok(
                    logs.Select(
                        log =>
                            new
                            {
                                id =
                                    log.Id,

                                medicationId =
                                    log.MedicationId,

                                taken =
                                    log.Taken,

                                notes =
                                    log.Notes,

                                takenAt =
                                    log.TakenAt
                            }
                    )
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
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // PATIENT: MARK TAKEN / SKIPPED
        // =====================================================

        [HttpPost(
            "{medicationId:guid}/log"
        )]
        [Authorize(
            Roles = RoleNames.Patient
        )]
        public async Task<IActionResult>
            LogMedication(
                Guid medicationId,
                MedicationLogRequestDto dto
            )
        {
            try
            {
                await _medicationService
                    .LogPatientMedicationAsync(
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
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        // =====================================================
        // PRIVACY
        // =====================================================

        private async Task<bool?>
            GetHealthDataSharingSettingAsync(
                Guid patientId
            )
        {
            var patient =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                                patientId
                    )
                    .Select(
                        item =>
                            new
                            {
                                Allowed =
                                    item.Preference ==
                                        null
                                        ? true
                                        : item
                                            .Preference
                                            .ShareHealthData
                            }
                    )
                    .FirstOrDefaultAsync();

            return patient?.Allowed;
        }

        private async Task<bool?>
            GetMedicationHealthDataSharingSettingAsync(
                Guid medicationId
            )
        {
            var medication =
                await _context.Medications
                    .AsNoTracking()
                    .Where(
                        item =>
                            item.Id ==
                                medicationId
                    )
                    .Select(
                        item =>
                            new
                            {
                                Allowed =
                                    item.Patient
                                        .Preference ==
                                        null
                                        ? true
                                        : item
                                            .Patient
                                            .Preference!
                                            .ShareHealthData
                            }
                    )
                    .FirstOrDefaultAsync();

            return medication?.Allowed;
        }

        // =====================================================
        // RESPONSE MAPPING
        // =====================================================

        private static object
            ToMedicationResponse(
                Medication medication
            )
        {
            return new
            {
                id =
                    medication.Id,

                patientId =
                    medication.PatientId,

                name =
                    medication.Name,

                dosage =
                    medication.Dosage,

                form =
                    medication.Form,

                instructions =
                    medication.Instructions,

                unitsPerDose =
                    medication.UnitsPerDose,

                prescribedBy =
                    medication.PrescribedBy,

                conditionName =
                    medication.ConditionName,

                startDate =
                    medication.StartDate,

                endDate =
                    medication.EndDate,

                isActive =
                    medication.IsActive,

                createdAt =
                    medication.CreatedAt,

                updatedAt =
                    medication.UpdatedAt,

                schedules =
                    medication.Schedules
                        .Select(
                            schedule =>
                                new
                                {
                                    id =
                                        schedule.Id,

                                    medicationId =
                                        schedule.MedicationId,

                                    timeOfDay =
                                        schedule.TimeOfDay,

                                    isActive =
                                        schedule.IsActive
                                }
                        )
                        .ToList(),

                logs =
                    medication.Logs
                        .OrderByDescending(
                            log =>
                                log.TakenAt
                        )
                        .Select(
                            log =>
                                new
                                {
                                    id =
                                        log.Id,

                                    medicationId =
                                        log.MedicationId,

                                    taken =
                                        log.Taken,

                                    notes =
                                        log.Notes,

                                    takenAt =
                                        log.TakenAt
                                }
                        )
                        .ToList()
            };
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
                    UnauthorizedAccessException();
            }

            return userId;
        }
    }
}
