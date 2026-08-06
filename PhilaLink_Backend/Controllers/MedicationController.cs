using Microsoft.AspNetCore.Mvc;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicationController : ControllerBase
    {
        private readonly IMedicationService _medicationService;

        public MedicationController(IMedicationService medicationService)
        {
            _medicationService = medicationService;
        }

        // CREATE MEDICATION
        [HttpPost]
        public async Task<IActionResult> Create(Guid patientId, string name, string dosage, string instructions)
        {
            var result = await _medicationService.CreateMedicationAsync(patientId, name, dosage, instructions);
            return Ok(result);
        }

        // GET PATIENT MEDICATIONS
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(Guid patientId)
        {
            return Ok(await _medicationService.GetPatientMedicationsAsync(patientId));
        }

        // ADD SCHEDULE
        [HttpPost("schedule")]
        public async Task<IActionResult> AddSchedule(Guid medicationId, string timeOfDay)
        {
            var result = await _medicationService.AddScheduleAsync(medicationId, timeOfDay);
            return Ok(result);
        }

        // LOG MEDICATION
        [HttpPost("log")]
        public async Task<IActionResult> Log(Guid medicationId, bool taken, string? notes)
        {
            var result = await _medicationService.LogMedicationAsync(medicationId, taken, notes);
            return Ok(result);
        }

        // GET LOGS
        [HttpGet("logs/{medicationId}")]
        public async Task<IActionResult> GetLogs(Guid medicationId)
        {
            return Ok(await _medicationService.GetMedicationLogsAsync(medicationId));
        }
    }
}