using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;

namespace PersonalProject.Controllers
{
    [ApiController]
    [Route("api/super-admin/accounts-paged")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminAccountsPagedController
        : ControllerBase
    {
        private readonly PhilaLinkDbContext _context;

        public SuperAdminAccountsPagedController(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts(
            [FromQuery] string? role = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            page = Math.Max(1, page);

            pageSize = Math.Clamp(
                pageSize <= 0 ? 20 : pageSize,
                1,
                100
            );

            string? normalizedRole = null;

            if (
                !string.IsNullOrWhiteSpace(role) &&
                !role.Equals(
                    "All",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                normalizedRole = RoleNames.All
                    .FirstOrDefault(
                        value => value.Equals(
                            role.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                if (normalizedRole == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid account role."
                    });
                }
            }

            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (normalizedRole != null)
            {
                query = query.Where(
                    user => user.Role == normalizedRole
                );
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var pattern = $"%{term}%";

                query = query.Where(
                    user =>
                        EF.Functions.ILike(
                            user.FullName,
                            pattern
                        )
                        ||
                        EF.Functions.ILike(
                            user.IdNumber,
                            pattern
                        )
                        ||
                        EF.Functions.ILike(
                            user.Email,
                            pattern
                        )
                        ||
                        EF.Functions.ILike(
                            user.PhoneNumber,
                            pattern
                        )
                        ||
                        EF.Functions.ILike(
                            user.Role,
                            pattern
                        )
                        ||
                        (
                            user.Admin != null &&
                            user.Admin.Clinic != null &&
                            EF.Functions.ILike(
                                user.Admin.Clinic.Name,
                                pattern
                            )
                        )
                        ||
                        (
                            user.Nurse != null &&
                            user.Nurse.Clinic != null &&
                            EF.Functions.ILike(
                                user.Nurse.Clinic.Name,
                                pattern
                            )
                        )
                        ||
                        (
                            user.Proxy != null &&
                            user.Proxy.Clinic != null &&
                            EF.Functions.ILike(
                                user.Proxy.Clinic.Name,
                                pattern
                            )
                        )
                        ||
                        (
                            user.Patient != null &&
                            user.Patient.Clinic != null &&
                            EF.Functions.ILike(
                                user.Patient.Clinic.Name,
                                pattern
                            )
                        )
                );
            }

            var totalItems = await query.CountAsync();

            var accounts = await query
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(
                    user => new SuperAdminAccountDto
                    {
                        UserId = user.Id,

                        FullName = user.FullName,

                        IdNumber = user.IdNumber,

                        Email = user.Email,

                        PhoneNumber = user.PhoneNumber,

                        Role = user.Role,

                        ClinicId =
                            user.Role == RoleNames.ClinicAdmin
                                ? user.Admin != null
                                    ? user.Admin.ClinicId
                                    : null
                                : user.Role == RoleNames.Nurse
                                    ? user.Nurse != null
                                        ? user.Nurse.ClinicId
                                        : null
                                    : user.Role == RoleNames.Proxy
                                        ? user.Proxy != null
                                            ? user.Proxy.ClinicId
                                            : null
                                        : user.Role == RoleNames.Patient
                                            ? user.Patient != null
                                                ? user.Patient.ClinicId
                                                : null
                                            : null,

                        ClinicName =
                            user.Role == RoleNames.ClinicAdmin
                                ? user.Admin != null &&
                                  user.Admin.Clinic != null
                                    ? user.Admin.Clinic.Name
                                    : null
                                : user.Role == RoleNames.Nurse
                                    ? user.Nurse != null &&
                                      user.Nurse.Clinic != null
                                        ? user.Nurse.Clinic.Name
                                        : null
                                    : user.Role == RoleNames.Proxy
                                        ? user.Proxy != null &&
                                          user.Proxy.Clinic != null
                                            ? user.Proxy.Clinic.Name
                                            : null
                                        : user.Role == RoleNames.Patient
                                            ? user.Patient != null &&
                                              user.Patient.Clinic != null
                                                ? user.Patient.Clinic.Name
                                                : null
                                            : null,

                        IsActive = user.IsActive,

                        IsVerified = user.IsVerified,

                        MustChangePassword =
                            user.MustChangePassword,

                        CreatedAt = user.CreatedAt,

                        UpdatedAt = user.UpdatedAt
                    }
                )
                .ToListAsync();

            return Ok(
                new PagedResultDto<SuperAdminAccountDto>
                {
                    Items = accounts,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            );
        }
    }
}
