using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/nurses")]
    [Authorize(
        Roles = RoleNames.Nurse
    )]
    public class NursesController :
        ControllerBase
    {
        private readonly INurseService
            _nurseService;

        public NursesController(
            INurseService nurseService
        )
        {
            _nurseService =
                nurseService;
        }

        // =====================================================
        // PROFILE
        // =====================================================

        [HttpGet("me")]
        public async Task<IActionResult>
            Me()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetMeAsync(
                            GetCurrentUserId()
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

        [HttpPut("me")]
        public async Task<IActionResult>
            UpdateMe(
                UpdateNurseProfileDto dto
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .UpdateMeAsync(
                            GetCurrentUserId(),
                            dto
                        )
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
        // DASHBOARD
        // =====================================================

        [HttpGet("me/dashboard")]
        public async Task<IActionResult>
            Dashboard()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetDashboardAsync(
                            GetCurrentUserId()
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

        [HttpGet("me/alerts")]
        public async Task<IActionResult>
            Alerts()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetUrgentAlertsAsync(
                            GetCurrentUserId()
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

        // =====================================================
        // PATIENTS
        // =====================================================

        [HttpGet("me/patients")]
        public async Task<IActionResult>
            Patients()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetClinicPatientsAsync(
                            GetCurrentUserId()
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

        [HttpGet(
            "me/patients/{patientId:guid}"
        )]
        public async Task<IActionResult>
            PatientCare(
                Guid patientId
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetPatientCareAsync(
                            GetCurrentUserId(),
                            patientId
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
        // CLINIC PROXIES
        // =====================================================

        [HttpGet("me/proxies")]
        public async Task<IActionResult>
            ClinicProxies()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetClinicProxiesAsync(
                            GetCurrentUserId()
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

        // =====================================================
        // ALLERGIES
        // =====================================================

        [HttpPost(
            "me/patients/{patientId:guid}/allergies"
        )]
        public async Task<IActionResult>
            CreateAllergy(
                Guid patientId,
                NurseAllergyWriteDto dto
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .CreateAllergyAsync(
                            GetCurrentUserId(),
                            patientId,
                            dto
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

        [HttpPut(
            "me/patients/{patientId:guid}/allergies/{allergyId:guid}"
        )]
        public async Task<IActionResult>
            UpdateAllergy(
                Guid patientId,
                Guid allergyId,
                NurseAllergyWriteDto dto
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .UpdateAllergyAsync(
                            GetCurrentUserId(),
                            patientId,
                            allergyId,
                            dto
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

        [HttpDelete(
            "me/patients/{patientId:guid}/allergies/{allergyId:guid}"
        )]
        public async Task<IActionResult>
            DeleteAllergy(
                Guid patientId,
                Guid allergyId
            )
        {
            try
            {
                await _nurseService
                    .DeleteAllergyAsync(
                        GetCurrentUserId(),
                        patientId,
                        allergyId
                    );

                return Ok(
                    new
                    {
                        message =
                            "Allergy removed."
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
        // MEDICAL CONDITIONS
        // =====================================================

        [HttpPost(
            "me/patients/{patientId:guid}/conditions"
        )]
        public async Task<IActionResult>
            CreateCondition(
                Guid patientId,
                NurseConditionWriteDto dto
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .CreateConditionAsync(
                            GetCurrentUserId(),
                            patientId,
                            dto
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

        [HttpPut(
            "me/patients/{patientId:guid}/conditions/{conditionId:guid}"
        )]
        public async Task<IActionResult>
            UpdateCondition(
                Guid patientId,
                Guid conditionId,
                NurseConditionWriteDto dto
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .UpdateConditionAsync(
                            GetCurrentUserId(),
                            patientId,
                            conditionId,
                            dto
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

        [HttpDelete(
            "me/patients/{patientId:guid}/conditions/{conditionId:guid}"
        )]
        public async Task<IActionResult>
            ArchiveCondition(
                Guid patientId,
                Guid conditionId
            )
        {
            try
            {
                await _nurseService
                    .ArchiveConditionAsync(
                        GetCurrentUserId(),
                        patientId,
                        conditionId
                    );

                return Ok(
                    new
                    {
                        message =
                            "Condition archived."
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
        // COLLECTION SCHEDULING
        // =====================================================

        [HttpPost(
            "me/patients/{patientId:guid}/collections"
        )]
        public async Task<IActionResult>
            ScheduleCollection(
                Guid patientId,
                NurseScheduleCollectionDto dto
            )
        {
            try
            {
                return Ok(
                    await _nurseService
                        .ScheduleCollectionAsync(
                            GetCurrentUserId(),
                            patientId,
                            dto
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
        // CURRENT USER
        // =====================================================

        private Guid
            GetCurrentUserId()
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
                throw new
                    UnauthorizedAccessException();
            }

            return userId;
        }
    }
}
