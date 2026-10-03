using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/clinics")]
    [Authorize]
    public class ClinicController :
        ControllerBase
    {
        private readonly IClinicService
            _service;

        private readonly IAuditLogService
            _audit;

        public ClinicController(
            IClinicService service,
            IAuditLogService audit
        )
        {
            _service =
                service;

            _audit =
                audit;
        }

        [HttpGet]
        public async Task<IActionResult>
            GetAll()
        {
            return Ok(
                await _service
                    .GetAllAsync()
            );
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult>
            GetById(
                Guid id
            )
        {
            var result =
                await _service
                    .GetByIdAsync(
                        id
                    );

            return result ==
                null
                ? NotFound()
                : Ok(
                    result
                );
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            Create(
                CreateClinicDto dto
            )
        {
            try
            {
                var result =
                    await _service
                        .CreateAsync(
                            dto
                        );

                await _audit
                    .LogAsync(
                        "ClinicCreated",
                        GetCurrentUserId(),
                        $"Clinic {result.Id} ({result.Name}) was created.",
                        result.Id
                    );

                return Ok(
                    result
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
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            Update(
                Guid id,
                UpdateClinicDto dto
            )
        {
            try
            {
                var result =
                    await _service
                        .UpdateAsync(
                            id,
                            dto
                        );

                await _audit
                    .LogAsync(
                        "ClinicUpdated",
                        GetCurrentUserId(),
                        $"Clinic {result.Id} ({result.Name}) was updated.",
                        result.Id
                    );

                return Ok(
                    result
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
        }

        [HttpPatch(
            "{id:guid}/deactivate"
        )]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            Deactivate(
                Guid id
            )
        {
            try
            {
                await _service
                    .DeactivateAsync(
                        id
                    );

                await _audit
                    .LogAsync(
                        "ClinicDeactivated",
                        GetCurrentUserId(),
                        $"Clinic {id} was deactivated.",
                        id
                    );

                return Ok(
                    new
                    {
                        message =
                            "Clinic deactivated."
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
        }

        [HttpPatch(
            "{id:guid}/activate"
        )]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            Activate(
                Guid id
            )
        {
            try
            {
                await _service
                    .ActivateAsync(
                        id
                    );

                await _audit
                    .LogAsync(
                        "ClinicActivated",
                        GetCurrentUserId(),
                        $"Clinic {id} was activated.",
                        id
                    );

                return Ok(
                    new
                    {
                        message =
                            "Clinic activated."
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
                throw new UnauthorizedAccessException(
                    "Invalid authentication token."
                );
            }

            return userId;
        }
    }
}
