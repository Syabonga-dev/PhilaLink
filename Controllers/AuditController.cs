using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditLogService _service;

        public AuditController(
            IAuditLogService service
        )
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(
            Roles =
                "SuperAdmin,ClinicAdmin,Nurse"
        )]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                return Ok(
                    await _service
                        .GetVisibleLogsAsync(
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