using Microsoft.AspNetCore.Mvc;
using PhilaLink_Backend.Models.DTOs;
using PhilaLink_Backend.Services.Interfaces;

namespace PhilaLink_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SymptomAssessmentController : ControllerBase
    {
        private readonly ISymptomAssessmentService _service;

        public SymptomAssessmentController(ISymptomAssessmentService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SymptomCreateDto dto)
        {
            var result = await _service.CreateAsync(dto.PatientId, dto.Symptoms);
            return Ok(result);
        }

        [HttpGet("{patientId}")]
        public async Task<IActionResult> GetByPatient(Guid patientId)
        {
            var result = await _service.GetByPatientAsync(patientId);
            return Ok(result);
        }
    }
}