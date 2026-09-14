using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/symptom-assessments")]
    [Authorize(Policy = "PatientOnly")]
    public class SymptomAssessmentController :
        ControllerBase
    {
        private readonly
            ISymptomAssessmentService _service;

        public SymptomAssessmentController(
            ISymptomAssessmentService service
        )
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            SymptomCreateDto dto
        )
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(
                        dto.Symptoms
                    )
                )
                {
                    return BadRequest(
                        new
                        {
                            message =
                                "Symptoms are required."
                        }
                    );
                }

                var result =
                    await _service
                        .CreateForPatientAsync(
                            GetCurrentUserId(),
                            dto.Symptoms
                        );

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            try
            {
                return Ok(
                    await _service
                        .GetMyAssessmentsAsync(
                            GetCurrentUserId()
                        )
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