using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinics")]
    [Authorize]
    public class ClinicController : ControllerBase
    {
        private readonly IClinicService _service;

        public ClinicController(
            IClinicService service
        )
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(
                await _service.GetAllAsync()
            );
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id
        )
        {
            var result =
                await _service.GetByIdAsync(id);

            return result == null
                ? NotFound()
                : Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Create(
            CreateClinicDto dto
        )
        {
            try
            {
                return Ok(
                    await _service.CreateAsync(
                        dto
                    )
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new { message = ex.Message }
                );
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateClinicDto dto
        )
        {
            try
            {
                return Ok(
                    await _service.UpdateAsync(
                        id,
                        dto
                    )
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
        }

        [HttpPatch("{id:guid}/deactivate")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            Deactivate(
                Guid id
            )
        {
            try
            {
                await _service
                    .DeactivateAsync(id);

                return Ok(
                    new
                    {
                        message =
                            "Clinic deactivated."
                    }
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
        }

        [HttpPatch("{id:guid}/activate")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            Activate(
                Guid id
            )
        {
            try
            {
                await _service
                    .ActivateAsync(id);

                return Ok(
                    new
                    {
                        message =
                            "Clinic activated."
                    }
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
        }
    }
}