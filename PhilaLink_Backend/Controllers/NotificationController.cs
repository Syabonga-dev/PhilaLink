using Microsoft.AspNetCore.Mvc;
using PhilaLink_Backend.Services.Interfaces;
using PhilaLink_Backend.Models.DTOs;

namespace PhilaLink_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationController(INotificationService service)
        {
            _service = service;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserNotifications(Guid userId)
        {
            var result = await _service.GetUserNotificationsAsync(userId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            await _service.CreateAsync(dto.UserId, dto.Message);
            return Ok("Notification created");
        }

        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _service.MarkAsReadAsync(id);
            return Ok("Marked as read");
        }
    }
}