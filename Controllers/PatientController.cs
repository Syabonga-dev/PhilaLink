using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        // Get all patients
        [HttpGet]
        public async Task<IActionResult> GetAllPatients()
        {
            return Ok(await _patientService.GetAllPatientsAsync());
        }

        // Get patient by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(Guid id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);

            if (patient == null)
                return NotFound("Patient not found");

            return Ok(patient);
        }

        // Get patient by User ID
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPatientByUserId(Guid userId)
        {
            var patient = await _patientService.GetPatientByUserIdAsync(userId);

            if (patient == null)
                return NotFound("Patient not found");

            return Ok(patient);
        }

        // Create patient
        [HttpPost]
        public async Task<IActionResult> CreatePatient(Patient patient)
        {
            var result = await _patientService.CreatePatientAsync(patient);

            return Ok(result);
        }

        // Update patient
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(
            Guid id,
            Patient patient)
        {
            var result = await _patientService.UpdatePatientAsync(id, patient);

            return Ok(result);
        }

        // Delete patient
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var result = await _patientService.DeletePatientAsync(id);

            return Ok(result);
        }
    }
}