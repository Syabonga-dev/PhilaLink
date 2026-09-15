using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(
            IAppointmentService appointmentService
        )
        {
            _appointmentService = appointmentService;
        }

        // =====================================================
        // CLINIC STAFF: ALL CLINIC APPOINTMENTS
        // =====================================================

        [HttpGet]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var results =
                    await _appointmentService
                        .GetClinicAppointmentsAsync(
                            GetCurrentUserId()
                        );

                return Ok(results);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // PATIENT: OWN APPOINTMENTS
        // =====================================================

        [HttpGet("me")]
        [Authorize(Roles = RoleNames.Patient)]
        public async Task<IActionResult>
            GetMyAppointments()
        {
            try
            {
                var results =
                    await _appointmentService
                        .GetPatientAppointmentsAsync(
                            GetCurrentUserId()
                        );

                return Ok(results);
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

        // =====================================================
        // CLINIC STAFF: GET ONE
        // =====================================================

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult> GetById(
            Guid id
        )
        {
            try
            {
                var result =
                    await _appointmentService
                        .GetByIdAsync(
                            id,
                            GetCurrentUserId()
                        );

                return result == null
                    ? NotFound()
                    : Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // CLINIC STAFF: CREATE
        // =====================================================

        [HttpPost]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult> Create(
            CreateAppointmentDto dto
        )
        {
            try
            {
                var result =
                    await _appointmentService
                        .CreateAsync(
                            dto,
                            GetCurrentUserId()
                        );

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result
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

        // =====================================================
        // CLINIC STAFF: UPDATE
        // =====================================================

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateAppointmentDto dto
        )
        {
            try
            {
                var result =
                    await _appointmentService
                        .UpdateAsync(
                            id,
                            dto,
                            GetCurrentUserId()
                        );

                return Ok(result);
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