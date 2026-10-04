using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/super-admin/accounts")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminAccountsController :
        ControllerBase
    {
        private readonly PhilaLinkDbContext
            _context;

        public SuperAdminAccountsController(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // SYSTEM-WIDE ACCOUNT DIRECTORY
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            GetAccounts(
                [FromQuery]
                string? role = null
            )
        {
            string? normalizedRole =
                null;

            if (
                !string.IsNullOrWhiteSpace(
                    role
                ) &&
                !role.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                normalizedRole =
                    RoleNames.All
                        .FirstOrDefault(
                            value =>
                                value.Equals(
                                    role,
                                    StringComparison
                                        .OrdinalIgnoreCase
                                )
                        );

                if (
                    normalizedRole ==
                    null
                )
                {
                    return BadRequest(
                        new
                        {
                            message =
                                "Invalid account role."
                        }
                    );
                }
            }

            var query =
                _context.Users
                    .AsNoTracking()
                    .AsQueryable();

            if (
                normalizedRole !=
                null
            )
            {
                query =
                    query.Where(
                        user =>
                            user.Role ==
                            normalizedRole
                    );
            }

            var accounts =
                await query
                    .OrderBy(
                        user =>
                            user.FullName
                    )
                    .Select(
                        user =>
                            new SuperAdminAccountDto
                            {
                                UserId =
                                    user.Id,

                                FullName =
                                    user.FullName,

                                IdNumber =
                                    user.IdNumber,

                                Email =
                                    user.Email,

                                PhoneNumber =
                                    user.PhoneNumber,

                                Role =
                                    user.Role,

                                ClinicId =
                                    user.Role ==
                                        RoleNames.ClinicAdmin
                                        ? user.Admin !=
                                            null
                                            ? user.Admin
                                                .ClinicId
                                            : null

                                        : user.Role ==
                                            RoleNames.Nurse
                                            ? user.Nurse !=
                                                null
                                                ? user.Nurse
                                                    .ClinicId
                                                : null

                                            : user.Role ==
                                                RoleNames.Proxy
                                                ? user.Proxy !=
                                                    null
                                                    ? user.Proxy
                                                        .ClinicId
                                                    : null

                                                : user.Role ==
                                                    RoleNames.Patient
                                                    ? user.Patient !=
                                                        null
                                                        ? user.Patient
                                                            .ClinicId
                                                        : null

                                                    : null,

                                ClinicName =
                                    user.Role ==
                                        RoleNames.ClinicAdmin
                                        ? user.Admin !=
                                                null &&
                                            user.Admin
                                                .Clinic !=
                                                null
                                            ? user.Admin
                                                .Clinic
                                                .Name
                                            : null

                                        : user.Role ==
                                            RoleNames.Nurse
                                            ? user.Nurse !=
                                                    null &&
                                                user.Nurse
                                                    .Clinic !=
                                                    null
                                                ? user.Nurse
                                                    .Clinic
                                                    .Name
                                                : null

                                            : user.Role ==
                                                RoleNames.Proxy
                                                ? user.Proxy !=
                                                        null &&
                                                    user.Proxy
                                                        .Clinic !=
                                                        null
                                                    ? user.Proxy
                                                        .Clinic
                                                        .Name
                                                    : null

                                                : user.Role ==
                                                    RoleNames.Patient
                                                    ? user.Patient !=
                                                            null &&
                                                        user.Patient
                                                            .Clinic !=
                                                            null
                                                        ? user.Patient
                                                            .Clinic
                                                            .Name
                                                        : null

                                                    : null,

                                IsActive =
                                    user.IsActive,

                                IsVerified =
                                    user.IsVerified,

                                MustChangePassword =
                                    user.MustChangePassword,

                                CreatedAt =
                                    user.CreatedAt,

                                UpdatedAt =
                                    user.UpdatedAt
                            }
                    )
                    .ToListAsync();

            return Ok(
                accounts
            );
        }
    }
}
