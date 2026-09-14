using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/audit")]
    [Authorize(Policy = "AdminOnly")]
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
        public async Task<IActionResult> GetAll()
        {
            var logs =
                await _service.GetLogsAsync();

            return Ok(logs);
        }
    }
}