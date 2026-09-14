using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

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
        public async Task<IActionResult> Create(MedicationCreateDto dto)
        {
            try
            {
                var result = await _medicationService.CreateMedicationAsync(dto.PatientId, dto.Name, dto.Dosage, dto.Instructions);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
        }

        [HttpGet("patient/{patientId:guid}")]
        public async Task<IActionResult> GetByPatient(Guid patientId)
        {
            var result = await _medicationService.GetPatientMedicationsAsync(patientId);

            return Ok(result);
        }

        [HttpPost("{medicationId:guid}/schedule")]
        public async Task<IActionResult> AddSchedule(Guid medicationId, MedicationScheduleDto dto)
        {
            if (
                dto.MedicationId != Guid.Empty &&
                dto.MedicationId != medicationId
            )
            {
                return BadRequest(new { message = "Medication ID mismatch." });
            }

            var result = await _medicationService.AddScheduleAsync(medicationId, dto.TimeOfDay);

            return Ok(new { message = result });
        }

        [HttpGet("{medicationId:guid}/logs")]
        public async Task<IActionResult> GetLogs(Guid medicationId)
        {
            return Ok(await _medicationService.GetMedicationLogsAsync(medicationId));
        }
    }
}