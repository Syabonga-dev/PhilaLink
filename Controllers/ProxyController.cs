using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/proxies")]
    [Authorize]
    public class ProxyController : ControllerBase
    {
        private readonly IProxyService
            _proxyService;

        public ProxyController(
            IProxyService proxyService
        )
        {
            _proxyService =
                proxyService;
        }

        [HttpPost("assign")]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult> Assign(
            AssignProxyDto dto
        )
        {
            try
            {
                await _proxyService.AssignProxyAsync(
                    dto.PatientId,
                    dto.ProxyId,
                    GetCurrentUserId()
                );

                return Ok(
                    new
                    {
                        message =
                            "Proxy assigned successfully."
                    }
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

        [HttpGet("patient/{patientId:guid}")]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult>
            GetPatientProxies(
                Guid patientId
            )
        {
            try
            {
                return Ok(
                    await _proxyService
                        .GetPatientProxiesAsync(
                            patientId,
                            GetCurrentUserId()
                        )
                );
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

        [HttpGet("me/patients")]
        [Authorize(Policy = "ProxyOnly")]
        public async Task<IActionResult>
            GetMyPatients()
        {
            try
            {
                return Ok(
                    await _proxyService
                        .GetMyPatientsAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "ClinicStaff")]
        public async Task<IActionResult> Remove(
            Guid id
        )
        {
            try
            {
                await _proxyService.RemoveProxyAsync(
                    id,
                    GetCurrentUserId()
                );

                return Ok(
                    new
                    {
                        message =
                            "Proxy removed successfully."
                    }
                );
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