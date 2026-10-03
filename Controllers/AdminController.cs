using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Services.Interfaces;
using System.Security.Claims;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController :
        ControllerBase
    {
        private readonly IAdminService
            _adminService;

        private readonly IProxyService
            _proxyService;

        private readonly IAuditLogService
            _audit;

        public AdminController(
            IAdminService adminService,
            IProxyService proxyService,
            IAuditLogService audit
        )
        {
            _adminService =
                adminService;

            _proxyService =
                proxyService;

            _audit =
                audit;
        }

        [HttpPost("clinic-admins")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult>
            RegisterClinicAdmin(
                RegisterClinicAdminDto dto
            )
        {
            try
            {
                var currentUserId =
                    GetCurrentUserId();

                var result =
                    await _adminService
                        .RegisterClinicAdminAsync(
                            dto,
                            currentUserId
                        );

                await _audit
                    .LogAsync(
                        "ClinicAdminCreated",
                        currentUserId,
                        $"Clinic Administrator account {result.UserId} ({result.FullName}) was created.",
                        dto.ClinicId
                    );

                return Ok(
                    result
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
                return Conflict(
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

        [HttpPost("nurses")]
        public async Task<IActionResult>
            RegisterNurse(
                RegisterNurseDto dto
            )
        {
            try
            {
                var currentUserId =
                    GetCurrentUserId();

                var result =
                    await _adminService
                        .RegisterNurseAsync(
                            dto,
                            currentUserId
                        );

                await _audit
                    .LogAsync(
                        "NurseAccountCreated",
                        currentUserId,
                        $"Nurse account {result.UserId} ({result.FullName}) was created.",
                        dto.ClinicId
                    );

                return Ok(
                    result
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
                return Conflict(
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

        [HttpPost("proxies")]
        public async Task<IActionResult>
            RegisterProxy(
                RegisterProxyDto dto
            )
        {
            try
            {
                var currentUserId =
                    GetCurrentUserId();

                var result =
                    await _adminService
                        .RegisterProxyAsync(
                            dto,
                            currentUserId
                        );

                await _audit
                    .LogAsync(
                        "ProxyAccountCreated",
                        currentUserId,
                        $"Proxy account {result.UserId} ({result.FullName}) was created.",
                        dto.ClinicId
                    );

                return Ok(
                    result
                );
            }
            catch (
                InvalidOperationException ex
            )
            {
                return Conflict(
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

        [HttpGet("dashboard")]
        public async Task<IActionResult>
            GetDashboard()
        {
            try
            {
                return Ok(
                    await _adminService
                        .GetDashboardAsync(
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpGet("accounts")]
        public async Task<IActionResult>
            ListAccounts(
                [FromQuery]
                string? role
            )
        {
            try
            {
                return Ok(
                    await _adminService
                        .ListAccountsAsync(
                            role,
                            GetCurrentUserId()
                        )
                );
            }
            catch (
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpPatch(
            "accounts/{userId:guid}/deactivate"
        )]
        public async Task<IActionResult>
            Deactivate(
                Guid userId
            )
        {
            try
            {
                var currentUserId =
                    GetCurrentUserId();

                await _adminService
                    .DeactivateAccountAsync(
                        userId,
                        currentUserId
                    );

                await _audit
                    .LogAsync(
                        "AccountDeactivated",
                        currentUserId,
                        $"Account {userId} was deactivated."
                    );

                return Ok(
                    new
                    {
                        message =
                            "Account deactivated."
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

        [HttpPatch(
            "accounts/{userId:guid}/activate"
        )]
        public async Task<IActionResult>
            Activate(
                Guid userId
            )
        {
            try
            {
                var currentUserId =
                    GetCurrentUserId();

                await _adminService
                    .ActivateAccountAsync(
                        userId,
                        currentUserId
                    );

                await _audit
                    .LogAsync(
                        "AccountActivated",
                        currentUserId,
                        $"Account {userId} was activated."
                    );

                return Ok(
                    new
                    {
                        message =
                            "Account activated."
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

        [HttpPost("proxy-links")]
        public async Task<IActionResult>
            AssignProxy(
                Guid patientId,
                Guid proxyId
            )
        {
            try
            {
                await _proxyService
                    .AssignProxyAsync(
                        patientId,
                        proxyId,
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

        /*
         * This endpoint now works for BOTH administrative
         * roles.
         *
         * ClinicAdmin still receives its clinic profile.
         * SuperAdmin receives a system-level profile without
         * a clinic assignment.
         */
        [HttpGet("me")]
        public async Task<IActionResult>
            Me()
        {
            try
            {
                if (
                    User.IsInRole(
                        RoleNames.SuperAdmin
                    )
                )
                {
                    return Ok(
                        new
                        {
                            userId =
                                GetCurrentUserId(),

                            fullName =
                                User.FindFirstValue(
                                    ClaimTypes.Name
                                ) ??
                                "Super Administrator",

                            email =
                                string.Empty,

                            clinicId =
                                (Guid?)null,

                            clinicName =
                                "PhilaLink administration",

                            isActive =
                                true,

                            role =
                                RoleNames.SuperAdmin
                        }
                    );
                }

                return Ok(
                    await _adminService
                        .GetClinicAdminMeAsync(
                            GetCurrentUserId()
                        )
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
                UnauthorizedAccessException
            )
            {
                return Forbid();
            }
        }

        [HttpGet("clinic-overview")]
        public async Task<IActionResult>
            ClinicOverview()
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
                UnauthorizedAccessException
            )
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
                string.IsNullOrWhiteSpace(
                    value
                ) ||
                !Guid.TryParse(
                    value,
                    out var userId
                )
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
