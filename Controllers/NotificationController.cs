using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Policy = "ClinicStaff")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateNotificationDto dto)
        {
            if (dto.UserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest(new { message = "UserId and message are required." });
            }

            await _service.CreateAsync(dto.UserId, dto.Message.Trim());

            return Ok(new { message = "Notification created." });
        }
    }
}