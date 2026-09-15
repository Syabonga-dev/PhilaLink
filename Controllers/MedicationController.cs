using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;
using PersonalProject.Models.Constants;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/medications")]
    [Authorize(Policy = "ClinicStaff")]
    public class MedicationController : ControllerBase
    {
        private readonly IMedicationService _medicationService;

        public MedicationController(IMedicationService medicationService)
        {
            _medicationService = medicationService;
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Nurse)]
        public async Task<IActionResult> Create(MedicationCreateDto dto)
        {
            try
            {
                return Ok(
                    await _medicationService
                        .CreateMedicationAsync(
                            dto,
                            GetCurrentUserId()
                        )
                );
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

        [HttpGet(
            "patient/{patientId:guid}"
        )]
        public async Task<IActionResult>
            GetByPatient(
                Guid patientId
            )
        {
            try
            {
                return Ok(
                    await _medicationService
                        .GetPatientMedicationsAsync(
                            patientId,
                            GetCurrentUserId()
                        )
                );
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

        [HttpPost("{medicationId:guid}/schedule")]
        [Authorize(Roles = RoleNames.Nurse)]
        public async Task<IActionResult> AddSchedule(Guid medicationId, MedicationScheduleDto dto)
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

        [HttpGet(
            "{medicationId:guid}/logs"
        )]
        public async Task<IActionResult> GetLogs(
            Guid medicationId
        )
        {
            try
            {
                return Ok(
                    await _medicationService
                        .GetMedicationLogsAsync(
                            medicationId,
                            GetCurrentUserId()
                        )
                );
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

        private Guid GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(value) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
            )
            {
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}