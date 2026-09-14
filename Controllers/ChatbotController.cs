using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/chatbot")]
    [Authorize(Policy = "PatientOnly")]
    public class ChatbotController :
        ControllerBase
    {
        private readonly IChatbotService
            _chatbotService;

        public ChatbotController(
            IChatbotService chatbotService
        )
        {
            _chatbotService =
                chatbotService;
        }

        [HttpPost("message")]
        public async Task<IActionResult>
            SendMessage(
                ChatbotMessageRequestDto dto
            )
        {
            try
            {
                return Ok(
                    await _chatbotService
                        .SendMessageAsync(
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
                        message = ex.Message
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

        [HttpGet("history")]
        public async Task<IActionResult>
            GetHistory()
        {
            try
            {
                var history =
                    await _chatbotService
                        .GetHistoryAsync(
                            GetCurrentUserId()
                        );

                return Ok(history);
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpDelete("history")]
        public async Task<IActionResult>
            ClearHistory()
        {
            try
            {
                await _chatbotService
                    .ClearHistoryAsync(
                        GetCurrentUserId()
                    );

                return Ok(
                    new
                    {
                        message =
                            "Chat history cleared."
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

        private Guid GetCurrentUserId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
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
                throw new UnauthorizedAccessException();
            }

            return userId;
        }
    }
}