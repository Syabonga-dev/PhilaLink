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
    public class SymptomAssessmentController : ControllerBase
    {
        private readonly ISymptomAssessmentService _service;

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
                            dto
                        );

                return Ok(
                    new
                    {
                        id =
                            result.Id,

                        patientId =
                            result.PatientId,

                        symptomsJson =
                            result.SymptomsJson,

                        result =
                            result.Result,

                        recommendation =
                            result.Recommendation,

                        createdAt =
                            result.CreatedAt
                    }
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
            catch (
                ArgumentException ex
            )
            {
                return BadRequest(
                    new
                    {
                        message = ex.Message
                    }
                );
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
        {
            try
            {
                var assessments =
                    await _service
                        .GetMyAssessmentsAsync(
                            GetCurrentUserId()
                        );

                return Ok(
                    assessments.Select(
                        assessment =>
                            new
                            {
                                id =
                                    assessment.Id,

                                patientId =
                                    assessment.PatientId,

                                symptomsJson =
                                    assessment.SymptomsJson,

                                result =
                                    assessment.Result,

                                recommendation =
                                    assessment.Recommendation,

                                createdAt =
                                    assessment.CreatedAt
                            }
                    )
                );
            }
            catch (
                UnauthorizedAccessException
            )
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
                string.IsNullOrWhiteSpace(
                    value
                ) ||
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
