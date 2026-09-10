using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IProxyService _proxyService;

        public AdminController(IAdminService adminService, IProxyService proxyService)
        {
            _adminService = adminService;
            _proxyService = proxyService;
        }

        [HttpPost("nurses")]
        public async Task<IActionResult> RegisterNurse(RegisterNurseDto dto)
        {
            try
            {
                var result = await _adminService.RegisterNurseAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPost("proxies")]
        public async Task<IActionResult> RegisterProxy(RegisterProxyDto dto)
        {
            try
            {
                var result = await _adminService.RegisterProxyAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            return Ok(await _adminService.GetDashboardAsync());
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> ListAccounts([FromQuery] string? role)
        {
            return Ok(await _adminService.ListAccountsAsync(role));
        }

        [HttpPatch("accounts/{userId}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid userId)
        {
            try
            {
                await _adminService.DeactivateAccountAsync(userId);
                return Ok(new { message = "Account deactivated." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("accounts/{userId}/activate")]
        public async Task<IActionResult> Activate(Guid userId)
        {
            try
            {
                await _adminService.ActivateAccountAsync(userId);
                return Ok(new { message = "Account activated." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("accounts/{userId}")]
        public async Task<IActionResult> Delete(Guid userId)
        {
            try
            {
                await _adminService.DeleteAccountAsync(userId);
                return Ok(new { message = "Account deleted." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // Admin-initiated proxy assignment — the nurse-initiated version
        // stays on ProxyController/api/Proxy/assign. Removal is shared:
        // DELETE /api/Proxy/{id} already works regardless of who assigned it.
        [HttpPost("proxy-links")]
        public async Task<IActionResult> AssignProxy([FromQuery] Guid patientId, [FromQuery] Guid proxyId)
        {
            var adminUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminUserIdClaim == null || !Guid.TryParse(adminUserIdClaim, out var adminUserId))
                return Unauthorized();

            var result = await _proxyService.AssignProxyByAdminAsync(patientId, proxyId, adminUserId);
            return Ok(result);
        }
    }
}