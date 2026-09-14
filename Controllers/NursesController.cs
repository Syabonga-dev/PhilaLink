using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Constants;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/nurses")]
    [Authorize(Roles = RoleNames.Nurse)]
    public class NursesController :
        ControllerBase
    {
        private readonly INurseService _nurseService;

        public NursesController(
            INurseService nurseService
        )
        {
            _nurseService =
                nurseService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                return Ok(
                    await _nurseService.GetMeAsync(
                        GetCurrentUserId()
                    )
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("me/dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetDashboardAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("me/patients")]
        public async Task<IActionResult> Patients()
        {
            try
            {
                return Ok(
                    await _nurseService
                        .GetClinicPatientsAsync(
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
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );

            if (
                string.IsNullOrWhiteSpace(claim) ||
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