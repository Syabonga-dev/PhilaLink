using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IProxyService _proxyService;

        public AdminController(IAdminService adminService, IProxyService proxyService)
        {
            _adminService = adminService;
            _proxyService = proxyService;
        }

        // =====================================================
        // REGISTER CLINIC ADMIN
        // =====================================================

        [HttpPost("clinic-admins")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> RegisterClinicAdmin(RegisterClinicAdminDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var result = await _adminService.RegisterClinicAdminAsync(dto, currentUserId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // REGISTER NURSE
        // =====================================================

        [HttpPost("nurses")]
        public async Task<IActionResult> RegisterNurse(RegisterNurseDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var result = await _adminService.RegisterNurseAsync(dto, currentUserId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    new { message = ex.Message }
                );
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // REGISTER PROXY
        // =====================================================

        [HttpPost("proxies")]
        public async Task<IActionResult> RegisterProxy(RegisterProxyDto dto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var result = await _adminService.RegisterProxyAsync(dto, currentUserId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    new { message = ex.Message }
                );
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var result = await _adminService.GetDashboardAsync(currentUserId);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // ACCOUNTS
        // =====================================================

        [HttpGet("accounts")]
        public async Task<IActionResult> ListAccounts([FromQuery] string? role)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var result = await _adminService.ListAccountsAsync(role, currentUserId);

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // =====================================================
        // DEACTIVATE
        // =====================================================

        [HttpPatch("accounts/{userId:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                await _adminService.DeactivateAccountAsync(userId, currentUserId);

                return Ok(new { message = "Account deactivated." });
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

        // =====================================================
        // ACTIVATE
        // =====================================================

        [HttpPatch("accounts/{userId:guid}/activate")]
        public async Task<IActionResult> Activate(Guid userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                await _adminService.ActivateAccountAsync(userId, currentUserId);

                return Ok(new { message = "Account activated." });
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

        // =====================================================
        // PROXY ASSIGNMENT
        // =====================================================

        [HttpPost("proxy-links")]
        public async Task<IActionResult> AssignProxy(Guid patientId, Guid proxyId)
        {
            try
            {
                var adminUserId = GetCurrentUserId();

                await _proxyService.AssignProxyAsync(
                    patientId,
                    proxyId,
                    adminUserId
                );

                return Ok(
                    new
                    {
                        message = "Proxy assigned successfully."
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

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            try
            {
                return Ok(
                    await _adminService
                        .GetClinicAdminMeAsync(
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

        [HttpGet("clinic-overview")]
        public async Task<IActionResult> ClinicOverview()
        {
            try
            {
                return Ok(
                    await _adminService
                        .GetClinicOverviewAsync(
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

        // =====================================================
        // JWT USER ID
        // =====================================================

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (
                string.IsNullOrWhiteSpace(value) ||
                !Guid.TryParse(value, out var userId)
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