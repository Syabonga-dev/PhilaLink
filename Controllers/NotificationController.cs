using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize(Policy = "ClinicStaff")]
    public class NotificationController :
        ControllerBase
    {
        private readonly INotificationService
            _service;

        public NotificationController(
            INotificationService service
        )
        {
            _service =
                service;
        }

        [HttpPost]
        public async Task<IActionResult>
            Create(
                CreateNotificationDto dto
            )
        {
            if (
                dto.UserId ==
                    Guid.Empty ||
                string.IsNullOrWhiteSpace(
                    dto.Message
                )
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            "Patient user ID and message are required."
                    }
                );
            }

            try
            {
                var created =
                    await _service
                        .CreateForPatientAsync(
                            dto.UserId,
                            dto.Message,
                            GetCurrentUserId()
                        );

                if (!created)
                {
                    return Ok(
                        new
                        {
                            created =
                                false,

                            message =
                                "The patient has disabled clinic notifications."
                        }
                    );
                }

                return Ok(
                    new
                    {
                        created =
                            true,

                        message =
                            "Notification created."
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

        private Guid
            GetCurrentUserId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes
                        .NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(
                    claim
                ) ||
                !Guid.TryParse(
                    claim,
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
